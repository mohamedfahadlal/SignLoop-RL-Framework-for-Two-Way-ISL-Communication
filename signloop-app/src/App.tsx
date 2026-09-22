import { useState, useRef, useEffect } from "react";
import { invoke } from "@tauri-apps/api/core";
// Direct module imports
import * as mpHolistic from "@mediapipe/holistic/holistic.js";
import * as mpCamera from "@mediapipe/camera_utils/camera_utils.js";
import * as mpDrawing from "@mediapipe/drawing_utils/drawing_utils.js";

// Safe constructor resolvers that check all possible export structures
const Holistic = 
  (mpHolistic as any).Holistic || 
  (mpHolistic as any).default?.Holistic || 
  (window as any).Holistic;

const POSE_CONNECTIONS = 
  (mpHolistic as any).POSE_CONNECTIONS || 
  (mpHolistic as any).default?.POSE_CONNECTIONS || 
  (window as any).POSE_CONNECTIONS;

const HAND_CONNECTIONS = 
  (mpHolistic as any).HAND_CONNECTIONS || 
  (mpHolistic as any).default?.HAND_CONNECTIONS || 
  (window as any).HAND_CONNECTIONS;

const Camera = 
  (mpCamera as any).Camera || 
  (mpCamera as any).default?.Camera || 
  (window as any).Camera;

const drawConnectors = 
  (mpDrawing as any).drawConnectors || 
  (mpDrawing as any).default?.drawConnectors || 
  (window as any).drawConnectors;

const drawLandmarks = 
  (mpDrawing as any).drawLandmarks || 
  (mpDrawing as any).default?.drawLandmarks || 
  (window as any).drawLandmarks;

import "./App.css";
function App() {
  const [activeChannel, setActiveChannel] = useState<'channel1' | 'channel2'>('channel1');
  const [translation, setTranslation] = useState<string>("Waiting for signs...");
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  // The rolling 30-frame coordinate buffer (State Space S_t)
  const stateBufferRef = useRef<number[][]>([]);

  // Flattens X, Y, Z for both hands and pose into a 1D array
  const extractCoordinates = (results: any) => {
    const pose = results.poseLandmarks 
      ? results.poseLandmarks.map((res: any) => [res.x, res.y, res.z]).flat() 
      : new Array(33 * 3).fill(0);

    const leftHand = results.leftHandLandmarks 
      ? results.leftHandLandmarks.map((res: any) => [res.x, res.y, res.z]).flat() 
      : new Array(21 * 3).fill(0);

    const rightHand = results.rightHandLandmarks 
      ? results.rightHandLandmarks.map((res: any) => [res.x, res.y, res.z]).flat() 
      : new Array(21 * 3).fill(0);

    return [...pose, ...leftHand, ...rightHand];
  };

  useEffect(() => {
    let camera: any = null;

    if (activeChannel === 'channel1' && videoRef.current && canvasRef.current) {
      const videoElement = videoRef.current;
      const canvasElement = canvasRef.current;
      const canvasCtx = canvasElement.getContext('2d');

      const holistic = new Holistic({
        locateFile: (file: string) => `https://cdn.jsdelivr.net/npm/@mediapipe/holistic/${file}`
      });

      holistic.setOptions({
        modelComplexity: 1,
        smoothLandmarks: true,
        enableSegmentation: false,
        refineFaceLandmarks: false,
        minDetectionConfidence: 0.5,
        minTrackingConfidence: 0.5
      });

      holistic.onResults((results: any) => {
        if (!canvasCtx || !canvasElement || !videoElement) return;

        canvasElement.width = videoElement.videoWidth;
        canvasElement.height = videoElement.videoHeight;

        canvasCtx.save();
        canvasCtx.clearRect(0, 0, canvasElement.width, canvasElement.height);

        // Draw skeletons
        if (results.poseLandmarks) {
          drawConnectors(canvasCtx, results.poseLandmarks, POSE_CONNECTIONS, { color: '#00FF00', lineWidth: 4 });
          drawLandmarks(canvasCtx, results.poseLandmarks, { color: '#FF0000', lineWidth: 2 });
        }
        if (results.leftHandLandmarks) {
          drawConnectors(canvasCtx, results.leftHandLandmarks, HAND_CONNECTIONS, { color: '#CC0000', lineWidth: 5 });
          drawLandmarks(canvasCtx, results.leftHandLandmarks, { color: '#00FF00', lineWidth: 2 });
        }
        if (results.rightHandLandmarks) {
          drawConnectors(canvasCtx, results.rightHandLandmarks, HAND_CONNECTIONS, { color: '#00CC00', lineWidth: 5 });
          drawLandmarks(canvasCtx, results.rightHandLandmarks, { color: '#FF0000', lineWidth: 2 });
        }
        canvasCtx.restore();

        // Extract coordinates and update the rolling buffer
        const frameData = extractCoordinates(results);
        stateBufferRef.current.push(frameData);

        if (stateBufferRef.current.length > 30) {
          stateBufferRef.current.shift();
        }

        // When the 30-frame rolling window is full, send it to Rust!
        if (stateBufferRef.current.length === 30) {
        const flatTensor = stateBufferRef.current.flat();
        
        invoke("run_model_inference", { coordinates: flatTensor })
          .then((res: any) => {
            console.log("Prediction from Rust backend:", res);
            // Update the state with the string returned by Rust
            setTranslation(res);
          })
          .catch((err) => console.error("Inference IPC error:", err));
      }
      });

      // Pull Camera from the global window object loaded via CDN
      
      
      // The Camera object is now provided by the local import at the top of the file
      camera = new Camera(videoElement, {
        onFrame: async () => {
          if (videoElement) {
            await holistic.send({ image: videoElement });
          }
        },
        width: 640,
        height: 480
      });
      camera.start();
    }

    return () => {
      if (camera) {
        camera.stop();
      }
    };
  }, [activeChannel]);

  return (
    <div className="dashboard-container">
      <nav className="sidebar">
        <h2>SignLoop XR</h2>
        <div className="nav-buttons">
          <button 
            className={activeChannel === 'channel1' ? 'active' : ''} 
            onClick={() => setActiveChannel('channel1')}
          >
            Channel 1: Sign → Text
          </button>
          <button 
            className={activeChannel === 'channel2' ? 'active' : ''} 
            onClick={() => setActiveChannel('channel2')}
          >
            Channel 2: Text → Sign
          </button>
        </div>
      </nav>

      <main className="content">
        {activeChannel === 'channel1' ? (
          <div className="channel-view">
            <h3>Channel 1: Perception Layer</h3>
            <div className="video-container">
              <video ref={videoRef} className="webcam-feed" />
              <canvas ref={canvasRef} className="landmark-overlay" />
            </div>
            <div className="output-console">
              {/* Dynamically render the live translation output */}
              <p className="subtitle-text">{translation}</p>
            </div>
          </div>
        ) : (
          <div className="channel-view">
            <h3>Channel 2: Rendering Layer</h3>
            <div className="input-console">
              <input type="text" placeholder="Type or speak a message..." />
              <button>Send to Avatar</button>
            </div>
            <div className="avatar-placeholder">
              <p>3D Avatar Animation Will Render Here</p>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

export default App;