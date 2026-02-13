# compare new face images with the currently displayed faces
# if they are not found in the current display, queue them for display
# this is run as an asychronous process to allow the face detection and 
# the display to operate independently
# the load of the new/old face comparisons is also independant
from deepface import DeepFace
from copy import deepcopy
import glob
import os
import time

def list_files_by_pattern(directory_path, pattern):
    full_pattern = os.path.join(directory_path, pattern)
    files = glob.glob(full_pattern)
    return [os.path.basename(f) for f in files if os.path.isfile(f)]

projectDir = "/media/paulvr/LaCie/activeProjects/faceDB/"
# tmp is where the face detection drops faces that it finds
tmpDir = projectDir + "tmp/"
# currentPhotos is where fotos of currently diaplayed faces are kept for comparison
currentDir = projectDir + "currentPhotos/"
clrPattrn = "*clr.png"
# readyDir is where texture ready face images are queued for display
readyDir = projectDir + "newList/"
# set up for image comparisons
backends = ['opencv', 'ssd', 'dlib', 'mtcnn', 'retinaface', 'mediapipe']
# false compares with currentPhotos and deletes matches - true saves all faces
saveAll = True

while True:
    time.sleep(1) # adjust to synchronize display and face detection
# tmp files dropped by face detector - clr files can be used for comparison 
    tmpFiles = list_files_by_pattern(tmpDir, clrPattrn)
    for tmpFile in tmpFiles: 
#        print("incoming: " + tmpFile)
# full path of new face to be compared
        etmpFile = tmpDir + tmpFile
# look in the current files to avoid duplicates overloading the display
        currentPhotos = list_files_by_pattern(currentDir, clrPattrn)
        for currentPhoto in currentPhotos:
#            print("current:" + currentPhoto)
            eCurrentPhoto = currentDir + currentPhoto
            try:
                result = DeepFace.verify(img1_path=eCurrentPhoto, img2_path=etmpFile, enforce_detection=False, detector_backend=backends[1])
#                print ("new: " + etmpFile + "  current:" + eCurrentPhoto)
#                print (result)
                if result['verified'] == False or saveAll: # did not find a face match so send it for display
    # send the new face  to the current files directory for future comparisons
                    clrDestination = currentDir + tmpFile
                    os.rename(etmpFile, clrDestination)
    # send the new face texture image to be displayed
                    textureDestination = readyDir + tmpFile
                    textureDestination = textureDestination.replace("clr", "")
                    textureSource = etmpFile.replace("clr", "")
                    os.rename(textureSource, textureDestination)
                    xxx = "moved "
                else: # delete the new face files if they match something being displayed
                    xxx = "deleted "
                    os.remove(etmpFile)
                    textureSource = etmpFile.replace("clr", "")
                    os.remove(textureSource)
                print(xxx + etmpFile)
                    
            except Exception as e:
                print(e)
                print("problem with file " + etmpFile + " " + eCurrentPhoto)

