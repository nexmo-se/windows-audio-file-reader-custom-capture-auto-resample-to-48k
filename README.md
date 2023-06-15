AudioFileReader
==============

AudioFileReader is a simple application that shows how to load audio files and resample to them to the proper audio format before publishing them.

All relevant code is in AudioFileCapture.cs

The function LoadFileToBeSent uses the Naudio Library to load an audio file. We then load file inside a queue.
The function StartAudioCapturer triggers the process of dequeueing the audio queue and finally calling the SendToAudioBus function to publish the audio
