// Native iOS audio session control for RIDE.
//
// iOS decides whether the hardware ring/silent switch silences an application from the
// category of its AVAudioSession. The Ambient and SoloAmbient categories are silenced by
// the switch; Playback and PlayAndRecord are not. An application that plays speech the
// user is expected to hear therefore declares PlayAndRecord, after which the switch no
// longer affects it and the application presents its own mute control instead.
//
// PlayAndRecord routes output to the receiver by default, so DefaultToSpeaker is required
// or playback is quiet and appears to come from the earpiece.
//
// Also exposes the session's output volume and a change notification for it, which allows
// an application to treat the user raising the hardware volume as intent to unmute.

#import <AVFoundation/AVFoundation.h>

typedef void (*RideOutputVolumeChangedCallback)(float volume);

static RideOutputVolumeChangedCallback g_volumeCallback = NULL;

/// Receives key-value observations for the audio session and forwards them to managed code.
@interface RideAudioSessionObserver : NSObject
@end

@implementation RideAudioSessionObserver

- (void)observeValueForKeyPath:(NSString *)keyPath
                      ofObject:(id)object
                        change:(NSDictionary *)change
                       context:(void *)context
{
    if ([keyPath isEqualToString:@"outputVolume"] && g_volumeCallback != NULL)
    {
        g_volumeCallback([[AVAudioSession sharedInstance] outputVolume]);
    }
}

@end

static RideAudioSessionObserver *g_observer = nil;

extern "C"
{
    /// Configures the shared audio session so playback is unaffected by the ring/silent
    /// switch while microphone capture remains available. Pass true for allowMixing to
    /// leave other applications' audio playing; false interrupts it.
    void RideAudioSession_Configure(bool allowMixing)
    {
        AVAudioSession *session = [AVAudioSession sharedInstance];

        AVAudioSessionCategoryOptions options =
            AVAudioSessionCategoryOptionDefaultToSpeaker |
            AVAudioSessionCategoryOptionAllowBluetooth;

        if (allowMixing)
        {
            options |= AVAudioSessionCategoryOptionMixWithOthers;
        }

        NSError *error = nil;
        if (![session setCategory:AVAudioSessionCategoryPlayAndRecord
                      withOptions:options
                            error:&error])
        {
            NSLog(@"[RideAudioSession] setCategory failed: %@", error.localizedDescription);
        }

        error = nil;
        if (![session setActive:YES error:&error])
        {
            NSLog(@"[RideAudioSession] setActive failed: %@", error.localizedDescription);
        }
    }

    /// The session's current output volume, from 0 to 1. This is the hardware volume level
    /// and is independent of the ring/silent switch, whose position iOS does not expose.
    float RideAudioSession_GetOutputVolume()
    {
        return [[AVAudioSession sharedInstance] outputVolume];
    }

    /// Registers a callback invoked whenever the output volume changes. Pass NULL to stop
    /// observing. Calling this again replaces any previous callback.
    void RideAudioSession_SetOutputVolumeCallback(RideOutputVolumeChangedCallback callback)
    {
        g_volumeCallback = callback;

        if (callback != NULL && g_observer == nil)
        {
            g_observer = [[RideAudioSessionObserver alloc] init];
            [[AVAudioSession sharedInstance] addObserver:g_observer
                                             forKeyPath:@"outputVolume"
                                                options:NSKeyValueObservingOptionNew
                                                context:NULL];
        }
        else if (callback == NULL && g_observer != nil)
        {
            [[AVAudioSession sharedInstance] removeObserver:g_observer forKeyPath:@"outputVolume"];
            g_observer = nil;
        }
    }
}
