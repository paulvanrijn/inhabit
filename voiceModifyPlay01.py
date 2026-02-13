import speech_recognition as sr
import whisper
import os
import pyttsx3
import random
from datetime import datetime
import glob
import time
from pedalboard import Pedalboard, Reverb, Delay
from pedalboard.io import AudioFile
import numpy as np
import pygame
import random

# sound for Inhabiting the invisible - OCADU Masters Thesis 2026
# Paul Van Rijn
#
#
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

# set up for adding effects to the voice 
def list_files_by_pattern(directory_path, pattern):
    full_pattern = os.path.join(directory_path, pattern)
    files = glob.glob(full_pattern)
    return [os.path.basename(f) for f in files if os.path.isfile(f)]

rawDir = projectDir + "raw/"
readyDir = projectDir + "ready/"
rawPattrn = "*.wav"
# set up pygame sound mixer
pygame.mixer.init()

# set up to play the sounds
#
# small class to organize the voice files for playing
#
class PlayingVoice:
    def __init__(self, wavFile):
        self.wavFile = wavFile  # Instance attribute
        self.sound = pygame.mixer.Sound(wavFile)
        self.mixChannel = pygame.mixer.find_channel(True) 
        self.sleepSeconds = 0.0
        
    def myChannel(self):
        return self.mixChannel
        
    def bark(self):
    # Check if the channel is busy
        if not self.mixChannel.get_busy():
            print("playing...")
            self.mixChannel.play(self.sound)
# set a random delay for the next play
            self.sleepSeconds = self.sound.get_length() \
                + random.uniform(self.sound.get_length() * 2.5, self.sound.get_length() * 6.0)


playedDir = projectDir + "played/"
readyDir = projectDir + "ready/"
readyPattrn = "*.wav"

voices = []
maxNumberVoices = 6

# main infinite loop

while True:

# use pedalboard to add effects

    time.sleep(1) # adjust to synchronize voice capture and sound output
# raw files dropped by voice detector
    rawFiles = list_files_by_pattern(rawDir, rawPattrn)

    for rawFile in rawFiles: 
        rawSource = rawDir + rawFile
        rawOld = projectDir + "rawOld/" + rawFile
        readyDestination = readyDir + rawFile

# cool reverb and echo and filtering goes here
        with AudioFile(rawSource, 'r') as f:
            audio = f.read(f.frames)
            samplerate = f.samplerate
            board = Pedalboard([
                Reverb(room_size=0.5, wet_level=0.3, dry_level=0.7),
                Delay(delay_seconds=0.2, feedback=0.2, mix=0.2)
                ])
            effected_audio = board(input_array=audio, sample_rate=samplerate)
            with AudioFile(readyDestination, 'w', samplerate) as f:
                f.write(effected_audio)
            if os.path.exists(rawSource): # clean up the old
                os.remove(rawSource)
        break # one file per pass

# play the files with a random delay

#
# files dropped by sound enhacer 
#
    readyFiles = list_files_by_pattern(readyDir, readyPattrn)

# add any new sound files to the play list
    for readyFile in readyFiles: 
        readySource = readyDir + readyFile
        playedDestination = playedDir + readyFile
        os.rename(readySource, playedDestination)
        voices.append(PlayingVoice(playedDestination))
#        break # one file per pass
    
# go through the list of voice files and play them as per delay
    for voice in voices:
        if len(voices) > maxNumberVoices: # clean up excess
            voices.remove(voice)
            
        if voice.sleepSeconds > 0.0:
            voice.sleepSeconds = voice.sleepSeconds - 1.0
        else:
            voice.bark()
            time.sleep(0.1)
