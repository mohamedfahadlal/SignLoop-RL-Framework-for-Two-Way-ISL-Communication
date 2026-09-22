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

import { Unity, useUnityContext } from "react-unity-webgl";

import "./App.css";
function App() {
  const [activeChannel, setActiveChannel] = useState<'channel1' | 'channel2'>('channel1');
  const [translation, setTranslation] = useState<string>("Waiting for signs...");
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  // Setup Unity WebGL Context
  const { unityProvider, sendMessage, addEventListener, removeEventListener } = useUnityContext({
    loaderUrl: "/Build/public.loader.js",
    dataUrl: "/Build/public.data.gz",
    frameworkUrl: "/Build/public.framework.js.gz",
    codeUrl: "/Build/public.wasm.gz",
  });

  const [currentSignCaption, setCurrentSignCaption] = useState("");

  // Listen for the two-way bridge events sent from the C# Avatar!
  useEffect(() => {
    const handleSignStarted = (signName: string) => {
      console.log("Avatar started signing:", signName);
      setCurrentSignCaption(signName);
    };
    const handleSignStopped = () => {
      console.log("Avatar stopped signing.");
      setCurrentSignCaption("");
    };

    addEventListener("OnSignStarted", handleSignStarted);
    addEventListener("OnSignStopped", handleSignStopped);
    return () => {
      removeEventListener("OnSignStarted", handleSignStarted);
      removeEventListener("OnSignStopped", handleSignStopped);
    };
  }, [addEventListener, removeEventListener]);

  // Keep a live reference to sendMessage so the microphone callback always has the latest bridge!
  const sendMessageRef = useRef(sendMessage);
  useEffect(() => {
    sendMessageRef.current = sendMessage;
  }, [sendMessage]);

  // Forcefully stop Unity from stealing global keystrokes!
  useEffect(() => {
    const stopUnityKeyboard = (e: KeyboardEvent) => {
      if (document.activeElement?.tagName === 'INPUT') {
        e.stopImmediatePropagation();
      }
    };
    // Capture phase (true) ensures this runs BEFORE Unity's Emscripten listeners!
    window.addEventListener('keydown', stopUnityKeyboard, true);
    window.addEventListener('keyup', stopUnityKeyboard, true);
    window.addEventListener('keypress', stopUnityKeyboard, true);
    
    return () => {
      window.removeEventListener('keydown', stopUnityKeyboard, true);
      window.removeEventListener('keyup', stopUnityKeyboard, true);
      window.removeEventListener('keypress', stopUnityKeyboard, true);
    };
  }, []);

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

  const [textInput, setTextInput] = useState("");
  const [isListening, setIsListening] = useState(false);
  const recognitionRef = useRef<any>(null);
  const silenceTimerRef = useRef<any>(null);
  const intentionallyListeningRef = useRef<boolean>(false);

  // Helper to format text and preserve ISL multi-word phrases!
  const formatISLSentence = (text: string) => {
    let formatted = text
      .split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');

    const islPhrases = [
      "Good Morning", "How Are You", "Small Little", "Store Or Shop",
      "Street Or Road", "Thank You", "Train Station", "Train Ticket"
    ];
    
    islPhrases.forEach(phrase => {
      formatted = formatted.replace(new RegExp(phrase, 'gi'), phrase.replace(/ /g, ""));
    });
    
    return formatted;
  };

  useEffect(() => {
    const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (SpeechRecognition && !recognitionRef.current) {
      const recognition = new SpeechRecognition();
      recognition.continuous = true;
      recognition.interimResults = true;
      recognition.lang = 'en-US';

      recognition.onresult = (event: any) => {
        let fullTranscript = "";
        for (let i = 0; i < event.results.length; i++) {
          fullTranscript += event.results[i][0].transcript;
        }
        setTextInput(fullTranscript);

        if (silenceTimerRef.current) clearTimeout(silenceTimerRef.current);
        
        silenceTimerRef.current = setTimeout(() => {
          if (fullTranscript.trim()) {
            const formattedSpeech = formatISLSentence(fullTranscript.trim());

            console.log("Voice-Activation Triggered:", formattedSpeech);
            sendMessageRef.current("ISL_AvatarRig", "PlaySentence", formattedSpeech);
            setTextInput(""); 
            
            recognition.stop();
          }
        }, 1500); 
      };

      recognition.onerror = (event: any) => {
        console.error("Speech recognition error", event.error);
        if (event.error !== 'aborted') {
          intentionallyListeningRef.current = false;
          setIsListening(false);
        }
      };

      recognition.onend = () => {
        // If we still want to be listening (e.g. we just rebooted it), turn it back on!
        if (intentionallyListeningRef.current) {
          try {
            recognition.start();
          } catch (e) { }
        } else {
          setIsListening(false);
        }
      };

      recognitionRef.current = recognition;
    }
  }, []);

  const handleSendToUnity = async () => {
    if (!textInput.trim()) return;
    
    // Stop listening when sending manually
    if (isListening && recognitionRef.current) {
      intentionallyListeningRef.current = false;
      recognitionRef.current.stop();
      setIsListening(false);
    }

    const formattedSentence = formatISLSentence(textInput);

    console.log("Sent to Unity WebGL:", formattedSentence);
    sendMessageRef.current("ISL_AvatarRig", "PlaySentence", formattedSentence);
    setTextInput(""); 
  };

  const handleVoiceInput = () => {
    if (!recognitionRef.current) {
      alert("Your browser does not support Web Speech API. Please use Chrome or Edge.");
      return;
    }

    if (isListening) {
      intentionallyListeningRef.current = false;
      recognitionRef.current.stop();
      setIsListening(false);
    } else {
      setTextInput(""); // Clear before speaking
      intentionallyListeningRef.current = true;
      try {
        recognitionRef.current.start();
      } catch (err) {
        console.warn("Speech recognition is already running in the background.");
      }
      setIsListening(true);
    }
  };

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
            <div className="header-row">
              <h3>Channel 2: XR Rendering Engine</h3>
              <div className="status-badge">🟢 Engine Online</div>
            </div>

            <div className="input-console">
              <input 
                type="text" 
                className="modern-input"
                placeholder="Type a sentence to translate (e.g. 'Hello how are you')..." 
                value={textInput}
                onChange={(e) => setTextInput(e.target.value)}
                onKeyDown={(e) => {
                  e.stopPropagation();
                  e.nativeEvent.stopImmediatePropagation();
                  if (e.key === 'Enter') handleSendToUnity();
                }}
                onKeyUp={(e) => {
                  e.stopPropagation();
                  e.nativeEvent.stopImmediatePropagation();
                }}
                onKeyPress={(e) => {
                  e.stopPropagation();
                  e.nativeEvent.stopImmediatePropagation();
                }}
              />
              <button className="modern-button send-btn" onClick={handleSendToUnity}>
                <span className="btn-icon">✨</span> Send to Avatar
              </button>
              <button 
                className={`modern-button mic-button ${isListening ? 'active-mic' : ''}`}
                onClick={handleVoiceInput}
              >
                {isListening ? "🎙️ Listening..." : "🎙️ Voice Input"}
              </button>
            </div>

            <div className="avatar-placeholder">
              <Unity unityProvider={unityProvider} style={{ width: "100%", height: "100%", display: "block" }} />
              
              {/* Dynamic VR Caption Overlay */}
              {currentSignCaption && (
                <div className="cinematic-caption">
                  {currentSignCaption}
                </div>
              )}
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

export default App;