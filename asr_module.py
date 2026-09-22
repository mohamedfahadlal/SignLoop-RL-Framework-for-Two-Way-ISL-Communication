import speech_recognition as sr
import whisper
import warnings

# Suppress minor warnings from Whisper
warnings.filterwarnings("ignore")

class SpeechToTextEngine:
    def __init__(self, model_size="base"):
        print(f"Loading Whisper '{model_size}' model...")
        # 'base' is incredibly fast and highly accurate for English.
        self.model = whisper.load_model(model_size)
        self.recognizer = sr.Recognizer()

    def listen_and_transcribe(self):
        with sr.Microphone() as source:
            print("\nAdjusting for ambient noise... Please wait.")
            self.recognizer.adjust_for_ambient_noise(source, duration=1)
            print("Listening! Speak an English sentence (e.g., 'The teacher is reading a book')...")
            
            try:
                # Capture the audio from the mic
                audio = self.recognizer.listen(source, timeout=5, phrase_time_limit=10)
                
                print("Processing audio...")
                # Save temporarily to feed to Whisper
                with open("temp_audio.wav", "wb") as f:
                    f.write(audio.get_wav_data())
                
                # Transcribe using Whisper
                result = self.model.transcribe("temp_audio.wav", fp16=False)
                english_text = result["text"].strip()
                
                print(f"\n[ASR Output]: {english_text}")
                return english_text
                
            except sr.WaitTimeoutError:
                print("No speech detected. Timed out.")
                return None
            except Exception as e:
                print(f"Error: {e}")
                return None

if __name__ == "__main__":
    asr = SpeechToTextEngine()
    asr.listen_and_transcribe()