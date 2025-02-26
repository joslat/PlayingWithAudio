# PlayingWithAudio
using tts and gpt-4o-audio to generate audio - and loooooong audio - from text ;)


## Motivation
I am building an Agentic AI assisted language coach application, to coach me in one of my biggest challenges: German Language ;)

I several Agentic workflows that define the learner profile, extract my intrinsic motivations, goals and scenarios, another that generates a long term plan and a last one that generates learning sessions.
A learning session can be at the moment of two types:
- a 1 to 1 session (conversation using the Realtime API)
- an Audio session meant to be listened to (not interactive)

This is about the second type, upon starting this repo, I had three problems:
- Tts and tts-hd was expiring in 1st March (today I checked and the date is changed to Feb 1, 2026)
- All the audio generation models only support 4096 characters.. which gives around 4-6 minutes of audio, depending on the speed.. not enough for an audiobook or an audio lesson...
- Tts & tts-hd seem to have a Max request per minute of 3... so you might want to use the preview model...

So I went to:
1. Split a longer text into 4096 chunks, convert into audio and put it together.
2. Try out the newer models, currently on preview: gpt-4o-audio-preview and gpt-4o-mini-audio-preview

Here is the code, fully working, for all of you dreaming of ai-generated audiobooks ;)
