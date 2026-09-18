import spacy

class ISLGrammarParser:
    def __init__(self):
        print("Loading spaCy NLP model...")
        # Load the English dependency parser
        self.nlp = spacy.load("en_core_web_sm")

    def translate_to_isl(self, english_text: str) -> list:
        """
        Converts standard SVO English to SOV ISL gloss syntax.
        """
        doc = self.nlp(english_text)
        
        subject_tokens = []
        object_tokens = []
        verb_tokens = []
        time_place_tokens = [] # For adverbs/time indicators
        
        # We omit auxiliary verbs (is, am, are) and determiners (a, an, the)
        ignore_pos = ["AUX", "DET", "PUNCT"]

        for token in doc:
            if token.pos_ in ignore_pos:
                continue
                
            # Lemmatization & Filtering: Convert to base forms and uppercase
            gloss = token.lemma_.upper()
            
            # Dependency Parsing: Identify Subject, Object, Verb
            if "subj" in token.dep_:
                subject_tokens.append(gloss)
            elif "obj" in token.dep_:
                object_tokens.append(gloss)
            elif token.pos_ == "VERB" or token.dep_ == "ROOT":
                verb_tokens.append(gloss)
            else:
                time_place_tokens.append(gloss)
                
        # Reordered ISL Gloss Sequence: Subject-Object-Verb
        isl_sequence = time_place_tokens + subject_tokens + object_tokens + verb_tokens
        
        return isl_sequence

if __name__ == "__main__":
    # Import your working ASR module
    from asr_module import SpeechToTextEngine
    
    # 1. Initialize both the ASR engine and the NLP Parser
    print("Initializing Head B Pipeline...")
    asr = SpeechToTextEngine(model_size="base")
    parser = ISLGrammarParser()
    
    # 2. Capture dynamic speech from the microphone
    print("\n--- Starting Live Translation Pipeline ---")
    english_sentence = asr.listen_and_transcribe()
    
    # 3. If speech was successfully captured, translate it into ISL grammar
    if english_sentence:
        isl_glosses = parser.translate_to_isl(english_sentence)
        print(f"\n[Spoken English (SVO)]: {english_sentence}")
        print(f"[ISL Gloss Array (SOV)]: {isl_glosses}")
    else:
        print("\n[Error]: No speech detected or translation failed. Please try again.")