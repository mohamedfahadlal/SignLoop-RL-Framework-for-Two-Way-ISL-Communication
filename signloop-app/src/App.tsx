import { useState, useRef, useEffect } from "react";
import { invoke } from "@tauri-apps/api/core";
import vocabData from "./vocab.json";
import { Unity, useUnityContext } from "react-unity-webgl";
import "./App.css";

// MediaPipe is loaded as scripts from index.html
const Holistic = (window as any).Holistic;
const Camera = (window as any).Camera;

const POSE_CONNECTIONS = (window as any).POSE_CONNECTIONS;
const HAND_CONNECTIONS = (window as any).HAND_CONNECTIONS;

const drawConnectors = (window as any).drawConnectors;
const drawLandmarks = (window as any).drawLandmarks;


// Helper: Normalizes a 30-frame sequence (30 frames x 75 landmarks x 3)
const normalizeSequence = (sequence: number[][]): number[] => {
  const normalized: number[] = [];

  for (let f = 0; f < sequence.length; f++) {
    const frame = sequence[f]; // 225 numbers (75 landmarks * 3)

    // Nose is landmark 0 (indices 0, 1, 2)
    const noseX = frame[0];
    const noseY = frame[1];
    const noseZ = frame[2];

    // Left shoulder is landmark 11 (indices 33, 34, 35)
    // Right shoulder is landmark 12 (indices 36, 37, 38)
    const dx = frame[33] - frame[36];
    const dy = frame[34] - frame[37];
    const dz = frame[35] - frame[38];
    const shoulderWidth = Math.sqrt(dx * dx + dy * dy + dz * dz) + 1e-6;

    // Center on nose and scale by shoulder width
    for (let i = 0; i < 75; i++) {
      const idx = i * 3;
      normalized.push((frame[idx] - noseX) / shoulderWidth);
      normalized.push((frame[idx + 1] - noseY) / shoulderWidth);
      normalized.push((frame[idx + 2] - noseZ) / shoulderWidth);
    }
  }

  return normalized;
};

// Helper: Resamples any arbitrary length frame sequence to exactly 30 frames
const resampleToWindow = (sequence: number[][], targetLength: number = 30): number[][] => {
  const n = sequence.length;
  if (n === targetLength) return sequence;
  if (n < 2) return sequence;

  const resampled: number[][] = [];
  for (let i = 0; i < targetLength; i++) {
    const idx = Math.floor((i * (n - 1)) / (targetLength - 1));
    resampled.push(sequence[idx]);
  }
  return resampled;
};

// Helper: Cleans dataset prefix numbers like "10. Energy" -> "Energy"
const cleanWord = (raw: string): string => {
  if (!raw) return "";
  const text = raw.includes(". ") ? raw.split(". ").slice(1).join(". ") : raw;
  return text.charAt(0).toUpperCase() + text.slice(1);
};



function App() {
  const [activeChannel, setActiveChannel] = useState<'channel1' | 'channel2'>('channel1');
  const [translation, setTranslation] = useState<string>("Waiting for signs...");
  const [confidence, setConfidence] = useState<number | null>(null);
  const [isRecording, setIsRecording] = useState<boolean>(false);
  
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const isInferringRef = useRef(false);
  const holisticRef = useRef<any>(null);

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
  const isRecordingRef = useRef(false);
  const recordBufferRef = useRef<number[][]>([]);

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

  const startRecording = () => {
    isRecordingRef.current = true;
    setIsRecording(true);
    recordBufferRef.current = [];
    setTranslation("Recording sign gesture...");
    setConfidence(null);
  };

  const stopRecordingAndInfer = async () => {
    if (!isRecordingRef.current) return;
    isRecordingRef.current = false;
    setIsRecording(false);

    const buffer = recordBufferRef.current;
    if (buffer.length < 10) {
      setTranslation("Gesture too short, please sign again");
      return;
    }

    setTranslation("Analyzing full gesture...");
    const resampled = resampleToWindow(buffer, 30);
    const flatTensor = normalizeSequence(resampled);

    try {
      const res: string = await invoke("run_model_inference", { coordinates: flatTensor });
      const data = JSON.parse(res);
      if (data.predicted_id >= 0) {
        const conf = Math.round(data.confidence * 100);
        setConfidence(conf);
        if (data.confidence >= 0.55) {
          const rawWord = vocabData[data.predicted_id] ?? `Class ID ${data.predicted_id}`;
          setTranslation(cleanWord(rawWord));
        } else {
          setTranslation(`Low confidence (${conf}%) - please repeat sign clearly`);
        }
      }
    } catch (err) {
      console.error("Inference IPC error:", err);
      setTranslation("Inference error");
    }
  };

  // Keyboard Spacebar hold-to-sign listener
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.code === "Space" && !e.repeat && activeChannel === "channel1") {
        e.preventDefault();
        startRecording();
      }
    };
    const handleKeyUp = (e: KeyboardEvent) => {
      if (e.code === "Space" && activeChannel === "channel1") {
        e.preventDefault();
        stopRecordingAndInfer();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    window.addEventListener("keyup", handleKeyUp);
    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      window.removeEventListener("keyup", handleKeyUp);
    };
  }, [activeChannel]);

  useEffect(() => {
    let camera: any = null;

    if (activeChannel === 'channel1' && videoRef.current && canvasRef.current) {
      const videoElement = videoRef.current;
      const canvasElement = canvasRef.current;
      const canvasCtx = canvasElement.getContext('2d');

      // Initialize Holistic only once across renders to avoid WASM MEMFS EEXIST collisions
      if (!holisticRef.current) {
        const h = new Holistic({
          locateFile: (file: string) => `/mediapipe/holistic/${file}`
        });

        h.setOptions({
          modelComplexity: 1,
          smoothLandmarks: true,
          enableSegmentation: false,
          refineFaceLandmarks: false,
          minDetectionConfidence: 0.5,
          minTrackingConfidence: 0.5
        });

        holisticRef.current = h;
      }

      const holistic = holisticRef.current;

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

        const frameData = extractCoordinates(results);

        // 1. Manual Recording Mode
        if (isRecordingRef.current) {
          recordBufferRef.current.push(frameData);
          return;
        }

        // 2. Automatic Continuous Gesture Mode
        const hasHands = Boolean(results.leftHandLandmarks || results.rightHandLandmarks);

        if (hasHands) {
          stateBufferRef.current.push(frameData);
          if (stateBufferRef.current.length > 45) {
            stateBufferRef.current.shift();
          }
        } else {
          // If hands were lowered and we had captured a full sign stroke (>= 20 frames):
          if (stateBufferRef.current.length >= 20 && !isInferringRef.current) {
            isInferringRef.current = true;
            const resampled = resampleToWindow(stateBufferRef.current, 30);
            const flatTensor = normalizeSequence(resampled);
            stateBufferRef.current = [];

            invoke("run_model_inference", { coordinates: flatTensor })
              .then((res: any) => {
                const data = JSON.parse(res);
                if (data.predicted_id >= 0) {
                  const conf = Math.round(data.confidence * 100);
                  setConfidence(conf);
                  if (data.confidence >= 0.60) {
                    const rawWord = vocabData[data.predicted_id] ?? `Class ID ${data.predicted_id}`;
                    setTranslation(cleanWord(rawWord));
                  }
                }
              })
              .catch((err) => console.error("Inference IPC error:", err))
              .finally(() => { isInferringRef.current = false; });
            return;
          }

          if (stateBufferRef.current.length > 0) {
            stateBufferRef.current.shift();
          }
        }

        // Continuous window trigger: when buffer reaches 40 frames of active hands
        if (
          stateBufferRef.current.length >= 40 &&
          hasHands &&
          !isInferringRef.current
        ) {
          isInferringRef.current = true;
          const resampled = resampleToWindow(stateBufferRef.current, 30);
          const flatTensor = normalizeSequence(resampled);

          invoke("run_model_inference", { coordinates: flatTensor })
            .then((res: any) => {
              const data = JSON.parse(res);
              if (data.predicted_id >= 0) {
                const conf = Math.round(data.confidence * 100);
                setConfidence(conf);
                if (data.confidence >= 0.65) {
                  const rawWord = vocabData[data.predicted_id] ?? `Class ID ${data.predicted_id}`;
                  setTranslation(cleanWord(rawWord));
                  stateBufferRef.current = []; // Clear on confident detection
                } else {
                  stateBufferRef.current = stateBufferRef.current.slice(15);
                }
              }
            })
            .catch((err) => console.error("Inference IPC error:", err))
            .finally(() => { isInferringRef.current = false; });
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
      return () => {
        if (camera) {
          camera.stop();
        }
      };
    }
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
              <video
              ref={videoRef}
              className="webcam-feed"
              autoPlay
              playsInline
              muted
            />
              <canvas ref={canvasRef} className="landmark-overlay" />
            </div>
            <div className="output-console">
              {/* Dynamically render the live translation output */}
              <p className="subtitle-text">{translation}</p>
              {confidence !== null && (
                <span className="confidence-badge">{confidence}% Conf</span>
              )}
            </div>
            <div className="controls-row">
              <button
                className={`record-btn ${isRecording ? "recording" : ""}`}
                onMouseDown={startRecording}
                onMouseUp={stopRecordingAndInfer}
                onTouchStart={startRecording}
                onTouchEnd={stopRecordingAndInfer}
              >
                {isRecording ? "● Recording Gesture... (Release to Translate)" : "🖐️ Hold Spacebar (or Click & Hold) to Sign"}
              </button>
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