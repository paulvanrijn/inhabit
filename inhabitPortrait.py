#
# take portraits and create Unity texture ready and face compare images
#
import cv2
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
from deepface import DeepFace
import numpy as np
import time
from datetime import datetime
from pathlib import Path

import poolConfig

# set up for face detection
mp_face_detection = mp.solutions.face_detection
face_detection =  mp_face_detection.FaceDetection(
    model_selection=0, min_detection_confidence=0.5)

#
# set up for making portraits
portraitHeight = 512
portraitWidth = 512
# set up for clearing the background
mp_selfie_segmentation = mp.solutions.selfie_segmentation
yetAnotherSelphi = mp_selfie_segmentation.SelfieSegmentation(model_selection=1)
portraitBack = np.zeros((portraitHeight, portraitWidth, 3), np.uint8)
portrait = np.zeros((portraitHeight, portraitWidth, 3), np.uint8)
portraitRaw = np.zeros((portraitHeight, portraitWidth, 3), np.uint8)

recognition_frame = None
#recogniecognition_frame = None
recognition_result_list = []

outPortraitMinNumber = 5
outPortraitNumber = outPortraitMinNumber
outPortraitMax = 6
#
# set up for saving portraits
projectDir = "/media/paulvr/LaCie/activeProjects/faceDB/"
tmpDir = projectDir + "tmp/"
DBdir = projectDir + "raw/"
pattrn = "*.png"


# callback for the asychronous gesture recognizer
def save_result(result: vision.GestureRecognizerResult, unused_output_image: mp.Image, timestamp_ms: int):
    recognition_result_list.append(result)
# end save results

# Create an GestureRecognizer object from google mediapipe
#
gBaseOptions = python.BaseOptions(model_asset_path='models/gesture_recognizer.task')
gestureOptions = vision.GestureRecognizerOptions(base_options=gBaseOptions,
                                          running_mode=vision.RunningMode.LIVE_STREAM,
                                          num_hands=1,
                                          min_hand_detection_confidence=0.5,
                                          min_hand_presence_confidence=0.5,
                                          min_tracking_confidence=0.5,
                                          result_callback=save_result)

handGestureRecognizer = vision.GestureRecognizer.create_from_options(gestureOptions)

# "posterise" 
def kwantyz(fromImage, toImage):       
        # test a quantization test
    n = 8    # Number of levels of quantization

    indices = np.arange(0,256)   # List of all colors 

    divider = np.linspace(0,255,n+1)[1] # we get a divider

    quantiz = np.int0(np.linspace(0,255,n)) # we get quantization colors

    color_levels = np.clip(np.int0(indices/divider),0,n-1) # color levels 0,1,2..

    palette = quantiz[color_levels] # Creating the palette

    toImage = palette[fromImage]  # Applying palette on image

    toImage = cv2.convertScaleAbs(toImage) # Converting image back to uint8
    return toImage
# end kwantyze
#
# save the raw portrait for later comparisons
#  
# save the portrait for later effects
#
def savePortrait(portraitRaw):
    global outPortraitNumber, outPortraitMax
    outPortraitNumber = outPortraitNumber + 1
    if outPortraitNumber > outPortraitMax:
        outPortraitNumber = outPortraitMinNumber
    
    stime = datetime.now().strftime("%Y%m%d%H%M%S")
    fname = projectDir + "tmp/" + stime + ".png"
    gname = projectDir + "tmp/" + stime + "clr.png"
 #   fname = poolConfig.pendingLocation + f"face{outPortraitNumber:02d}" + ".png"
#    gname = poolConfig.pendingLocation + f"grayface{outPortraitNumber:02d}" + ".png"

    # remove the background
    portraitRaw.flags.writeable = False
    selfieResults = yetAnotherSelphi.process(portraitRaw)  
    condition = np.stack((selfieResults.segmentation_mask,) * 3, axis=-1) > 0.1
    output_image = np.where(condition, portraitRaw, portraitBack)
        
# colour quantization 
    portrait = kwantyz(output_image, portraitRaw)
# make it gray and transparent
    grayImage = cv2.cvtColor(portrait, cv2.COLOR_BGR2GRAY)
    portraitRgba = cv2.cvtColor(grayImage, cv2.COLOR_RGB2RGBA)
    portraitRgba[:,:,3] = grayImage
    rotatedImage = cv2.rotate(portraitRgba, cv2.ROTATE_180) #rotate 180 for unity

    if cv2.imwrite(fname, rotatedImage):
# write a  version for deepface comparison
        cv2.imwrite(gname, portraitRaw)
    else:
        print("write failed")
        print(fname)

# end Save portrait

# cut out face - returns face ROI with a margin

def cutOutFace(bbox_data, frame):
  # all in normalized to frame
    h, w, z = frame.shape
    y_min = int(bbox_data.ymin * h)
    x_min = int(bbox_data.xmin * w)
    boxw = int(bbox_data.width * w)
    boxh = int(bbox_data.height * h)
#            print(f"y {y_min} height {boxh} --- w {w} width {bbox_data.width}")
    boxWMargin = int(boxw * 0.15) # adjust to make beautiful
    boxHMargin = int(boxh * 0.25) # adjust to make beautiful
    
    cuty =  y_min - boxHMargin
    if cuty < 0:
        cuty = 0
    cutx =  x_min - boxWMargin
    if cutx < 0:
        cutx = 0
    cuth = cuty + boxh  +  2 * boxHMargin
    cutw = cutx + boxw  + 2 * boxWMargin
    if cutw > w:
        cutw = w
    if cuty > h:
        cuth = h
# cut it out
    portraitRoi = frame[cuty:cuth, cutx:cutw]
    return portraitRoi


# drawing stick figures etc
mp_drawing = mp.solutions.drawing_utils
# set up to throttle face images
lastFaceDetection = datetime.now()
secsBetweenCaptures = 1.0

# set up capture
cap = cv2.VideoCapture(2) #0 laptop 1 usb cam
if cap.isOpened():
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)

while cap.isOpened():
    success, frame = cap.read()
    frame = cv2.flip(frame, 1) # SELPHI OF DELFI
    if True:
        if not success:
            print("Ignoring empty camera frame.")
            continue
    # wait after the last successful face detection
        timeNow = datetime.now()
        timeGap = timeNow - lastFaceDetection
        timeGapSeconds = timeGap.total_seconds()
        if timeGapSeconds < secsBetweenCaptures:
            time.sleep(0.2)
            continue

    # pass by reference.
        frame.flags.writeable = False
        frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
#
# look for faces
#
        faceDetectionResults = face_detection.process(frame)
    # Draw the face detection annotations on the image.
        frame.flags.writeable = True
        frame = cv2.cvtColor(frame, cv2.COLOR_RGB2BGR)
#
# set up for finding the portrait face
#
        largestFaceSize = 0
        largestFace = None

        if faceDetectionResults.detections:
            for faceDetection in faceDetectionResults.detections:
                bbox_data = faceDetection.location_data.relative_bounding_box
                h, w, z = frame.shape
                boxw = int(bbox_data.width * w)
                boxh = int(bbox_data.height * h)
                faceSize = boxw * boxh
#
# find the largest face
#
                if faceSize > largestFaceSize:
                    largestFace = faceDetection
                    largestFaceSize = faceSize
                
#
# check to see if the largest face is portrait worthy
#
# make a box round the face for the portrait and clip it out
#
# biggest face detected
#
        if largestFace != None:   
            portraitRoi = cutOutFace(largestFace.location_data.relative_bounding_box, frame)
  #          cv2.imshow('ROI', portraitRoi)
# project it to the final portrait size
            portraitRaw = cv2.resize(portraitRoi, (portraitHeight, portraitWidth))
#            cv2.imshow('resized', portraitRaw)

#
# collect info to decide if we want to keep the portrait
#
            try:
# emotion
                now_analysis = DeepFace.analyze(portraitRaw, actions=['emotion'], enforce_detection=False)
                dominant_emotion = now_analysis[0]['dominant_emotion']
            
            except Exception as e:
                print(f"Error during DeepFace analysis: {e}") # Uncomment for debugging
                pass
#
# decide if this is a good portrait
#
            if dominant_emotion == "happy" or dominant_emotion == "surprise":
#
#     the portrait is acceptable
                lastFaceDetection  = datetime.now()
#
# add stick figure to the frame to visualize
#                mp_drawing.draw_detection(frame, largestFace)
                savePortrait(portraitRaw)
#               print(category_name)
            recognition_result_list.clear()
    cv2.imshow('portrait', portraitRaw)
        
# Flip the image horizontally for a selfie-view display.
    cv2.imshow('MediaPipe Face Detection', frame)
    if cv2.waitKey(5) & 0xFF == 27: #esc key
      break
cap.release()
