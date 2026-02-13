import speech_recognition as sr
import whisper
import os
import pyttsx3
import random
from datetime import datetime

# listen at the microphone and create speech files
#https://github.com/openai/whisper/discussions/1406
# https://pyttsx3.readthedocs.io/en/latest/engine.html#module-pyttsx3.voice
# Initialize the text-to-speech engine
engine = pyttsx3.init()
# Optional: Adjust speech properties (rate, volume, voice)
engine.setProperty('rate', 150)  # Speed in words per minute
engine.setProperty('volume', 0.9) # Volume (0.0 to 1.0)
#voices = engine.getProperty('voices')
#thisVoice = random.randint(22, 29)
#voice = voices[thisVoice]
#engine.setProperty('voice', voice.id)
#numVoices = len(voices)

#for voice in voices:
 #   print(voice.name)
    
# Initialize the recognizer and load the Whisper model
r = sr.Recognizer()
# Choose a model size (tiny, base, small, medium, large)
model = whisper.load_model("base") 
projectDir = "/media/paulvr/LaCie/activeProjects/voiceDB/"

while True:
    print("Model loaded. Speak something into the microphone...")

    with sr.Microphone() as source:
    # Optional: adjust for ambient noise
        r.adjust_for_ambient_noise(source) 
        audio = r.listen(source)

    print("Audio captured. Transcribing...")

    try:
        # Transcribe the audio using the local Whisper model
        # Note: the speech_recognition library has a built-in method for this
        text = r.recognize_whisper(audio, model="base") 
        if len(text) > 0 and "1.5%" not in text :
            print(f"Transcription: {text}")
            stime = datetime.now().strftime("%Y%m%d%H%M%S")
            fname = projectDir + "raw/" + stime + ".wav"
            engine.save_to_file(text, fname)
            engine.runAndWait()
# Run the speech synthesis and wait for it to complete
    except sr.UnknownValueError:
        print("Sorry, could not understand audio")
    except sr.RequestError as e:
        print(f"Error with the transcription service; {e}")


