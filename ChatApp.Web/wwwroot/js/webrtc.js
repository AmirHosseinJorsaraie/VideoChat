// wwwroot/js/webrtc.js
// Handles all browser-side WebRTC logic.
// Blazor (VideoCallStateService) calls these functions via JS interop,
// and this file calls back into .NET to relay SDP/ICE via SignalR.

window.webRTC = (() => {
    let peerConnection = null;
    let localStream = null;

    const iceServers = {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' }
            // Add TURN servers here for production (needed when P2P fails through NAT)
            // { urls: 'turn:your-turn-server.com', username: '...', credential: '...' }
        ]
    };

    function createPeerConnection(callId, dotNetRef) {
        peerConnection = new RTCPeerConnection(iceServers);

        // Send ICE candidates to the other peer via .NET/SignalR
        peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                dotNetRef.invokeMethodAsync('SendIceCandidateAsync', {
                    callId: callId,
                    candidate: event.candidate.candidate,
                    sdpMid: event.candidate.sdpMid,
                    sdpMLineIndex: event.candidate.sdpMLineIndex
                });
            }
        };

        // Attach remote stream to the <video> element
        peerConnection.ontrack = (event) => {
            const remoteVideo = document.getElementById('remoteVideo');
            if (remoteVideo && event.streams[0]) {
                remoteVideo.srcObject = event.streams[0];
            }
        };

        peerConnection.onconnectionstatechange = () => {
            console.log('[WebRTC] Connection state:', peerConnection.connectionState);
        };

        return peerConnection;
    }

    async function getLocalStream() {
        if (localStream) return localStream;
        localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });

        const localVideo = document.getElementById('localVideo');
        if (localVideo) localVideo.srcObject = localStream;

        return localStream;
    }

    return {
        // Called on the CALLER side after callee accepts
        createOffer: async (callId, dotNetRef) => {
            const stream = await getLocalStream();
            const pc = createPeerConnection(callId, dotNetRef);
            stream.getTracks().forEach(track => pc.addTrack(track, stream));

            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);

            dotNetRef.invokeMethodAsync('SendOfferAsync', {
                callId: callId,
                type: offer.type,
                sdp: offer.sdp
            });
        },

        // Called on the CALLEE side when offer arrives
        handleOffer: async (signal, dotNetRef) => {
            const stream = await getLocalStream();
            const pc = createPeerConnection(signal.callId, dotNetRef);
            stream.getTracks().forEach(track => pc.addTrack(track, stream));

            await pc.setRemoteDescription({ type: signal.type, sdp: signal.sdp });

            const answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);

            dotNetRef.invokeMethodAsync('SendAnswerAsync', {
                callId: signal.callId,
                type: answer.type,
                sdp: answer.sdp
            });
        },

        // Called on the CALLER side when answer arrives
        handleAnswer: async (signal) => {
            if (peerConnection) {
                await peerConnection.setRemoteDescription({ type: signal.type, sdp: signal.sdp });
            }
        },

        // Called on both sides as ICE candidates arrive
        handleIceCandidate: async (candidate) => {
            if (peerConnection && candidate.candidate) {
                await peerConnection.addIceCandidate({
                    candidate: candidate.candidate,
                    sdpMid: candidate.sdpMid,
                    sdpMLineIndex: candidate.sdpMLineIndex
                });
            }
        },

        // Called when call ends — clean up
        closeConnection: () => {
            if (peerConnection) {
                peerConnection.close();
                peerConnection = null;
            }
            if (localStream) {
                localStream.getTracks().forEach(t => t.stop());
                localStream = null;
            }
            const localVideo = document.getElementById('localVideo');
            const remoteVideo = document.getElementById('remoteVideo');
            if (localVideo) localVideo.srcObject = null;
            if (remoteVideo) remoteVideo.srcObject = null;
        }
    };
})();
