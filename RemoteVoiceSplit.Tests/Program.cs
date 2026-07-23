using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RemoteVoiceSplit.AudioHost;
using RemoteVoiceSplit.AudioHost.Interop.WindowsAudio;
using RemoteVoiceSplit.Core;
using RemoteVoiceSplit.Interop.ProcessAudio;

namespace RemoteVoiceSplit.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length < 4)
            {
                throw new ArgumentException(
                    "Pass the built plugin DLL, audio host EXE, project version, artifact version, an optional package ZIP, and optional live-audio flags.",
                    nameof(args));
            }

            string assemblyPath = Path.GetFullPath(args[0]);
            string audioHostPath = Path.GetFullPath(args[1]);
            string expectedVersion = args[2];
            string expectedArtifactVersion = args[3];
            string? archivePath = null;
            bool runLiveAudio = false;
            int liveAudioSoakSeconds = 0;
            for (int index = 4; index < args.Length; index++)
            {
                if (string.Equals(args[index], "--live-audio", StringComparison.Ordinal))
                {
                    runLiveAudio = true;
                    continue;
                }

                if (string.Equals(
                        args[index],
                        "--live-audio-soak-seconds",
                        StringComparison.Ordinal))
                {
                    if (index + 1 >= args.Length ||
                        !int.TryParse(args[++index], out liveAudioSoakSeconds) ||
                        liveAudioSoakSeconds <= 0)
                    {
                        throw new ArgumentException(
                            "--live-audio-soak-seconds requires a positive integer.",
                            nameof(args));
                    }

                    runLiveAudio = true;
                    continue;
                }

                if (archivePath is not null)
                {
                    throw new ArgumentException("Only one package ZIP may be supplied.", nameof(args));
                }

                archivePath = Path.GetFullPath(args[index]);
            }

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("Built plugin DLL was not found.", assemblyPath);
            }

            if (!File.Exists(audioHostPath))
            {
                throw new FileNotFoundException("Built audio host EXE was not found.", audioHostPath);
            }

            if (archivePath is not null && !File.Exists(archivePath))
            {
                throw new FileNotFoundException("Package ZIP was not found.", archivePath);
            }

            RejectsInvalidCapacity();
            ConvertsMonoAndPreservesWrappedOrder();
            WritesOnlyRequestedSourceSamples();
            RejectsOverflowWithoutPartialWrite();
            MixesAndClampsConcurrentStreams();
            ClearDropsQueuedSamples();
            ConcurrentClearCannotRestoreReadCursor();
            ReactivationInvalidatesCapturedRegistration();
            RetirementWaitsForActiveCommit();
            RoutingRetirementWaitsForActiveSubmissions();
            RemoteVoiceSelectionCoversSupportedGamePaths();
            RemoteVoiceFallbackDefaultsToSilence();
            RoutingSessionRetiresAndRecovers();
            PluginRuntimeSurvivesComponentDestruction(assemblyPath);
            PluginConfigurationDefaultsToSilentFallback(assemblyPath);
            PluginConfigurationUpdatesFallbackLive(assemblyPath);
            VoiceFallbackBoundaryUsesPolicy(assemblyPath);
            AudioHostWindowTitleIsStable();
            AudioHostProtocolRoundTrips();
            ProcessAncestryHandlesTreesAndCycles();
            PcmAudioBufferReadsAndClearsPartialFrames();
            Console.WriteLine(
                "All deterministic fallback, routing, game-path, protocol, ancestry, and host-buffer tests passed.");

            if (runLiveAudio)
            {
                DefaultEndpointChangeRetiresAndRecovers();
                AudioHostReconnectCrashAndRestart(
                    audioHostPath,
                    TimeSpan.FromSeconds(liveAudioSoakSeconds));
                Console.WriteLine(
                    "Live default-endpoint, stable-host reconnect, crash, and recovery tests passed.");
            }

            PackageContractTests.Run(
                assemblyPath,
                audioHostPath,
                Directory.GetCurrentDirectory(),
                expectedVersion,
                expectedArtifactVersion,
                archivePath);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void RejectsInvalidCapacity()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => _ = new AudioRingBuffer(12));
    }

    private static void PluginRuntimeSurvivesComponentDestruction(string assemblyPath)
    {
        using ModuleDefinition module = ModuleDefinition.ReadModule(assemblyPath);
        TypeDefinition plugin = module.Types.Single(
            type => string.Equals(type.FullName, "RemoteVoiceSplit.Plugin", StringComparison.Ordinal));
        Assert(
            plugin.Methods.All(method => !string.Equals(method.Name, "OnDestroy", StringComparison.Ordinal)),
            "The BepInEx component must not tear down process-lifetime routing from OnDestroy.");

        TypeDefinition runtime = module.Types.Single(
            type => string.Equals(
                type.FullName,
                "RemoteVoiceSplit.Interop.Game.PluginRuntime",
                StringComparison.Ordinal));
        MethodDefinition initialize = runtime.Methods.Single(
            method => string.Equals(method.Name, "Initialize", StringComparison.Ordinal));
        MethodDefinition quitting = runtime.Methods.Single(
            method => string.Equals(
                method.Name,
                "OnApplicationQuitting",
                StringComparison.Ordinal));

        Assert(
            Calls(initialize, "UnityEngine.Application", "add_quitting"),
            "The process-lifetime runtime did not subscribe to the application quit event.");
        Assert(
            Calls(quitting, "RemoteVoiceSplit.Interop.Game.IntegrationContext", "Clear"),
            "Application quit did not clear game integration.");

        MethodDefinition unpatch = runtime.Methods.Single(
            method => string.Equals(method.Name, "TryUnpatch", StringComparison.Ordinal));
        Assert(
            Calls(unpatch, "HarmonyLib.Harmony", "UnpatchSelf"),
            "Application quit did not remove Harmony patches.");

        MethodDefinition dispose = runtime.Methods.Single(
            method => string.Equals(
                method.Name,
                "TryDisposeRouter",
                StringComparison.Ordinal));
        Assert(
            Calls(dispose, "RemoteVoiceSplit.Interop.ProcessAudio.VoiceProcessRouter", "Dispose"),
            "Application quit did not stop the process-audio router.");
    }

    private static bool Calls(
        MethodDefinition method,
        string declaringType,
        string methodName)
    {
        return method.Body.Instructions.Any(
            instruction =>
                instruction.OpCode.Code is Code.Call or Code.Callvirt &&
                instruction.Operand is MethodReference called &&
                string.Equals(
                    called.DeclaringType.FullName,
                    declaringType,
                    StringComparison.Ordinal) &&
                string.Equals(called.Name, methodName, StringComparison.Ordinal));
    }

    private static bool CallsGenericType(
        MethodDefinition method,
        string declaringType,
        string methodName)
    {
        return method.Body.Instructions.Any(
            instruction =>
                instruction.OpCode.Code is Code.Call or Code.Callvirt &&
                instruction.Operand is MethodReference called &&
                string.Equals(
                    called.DeclaringType.GetElementType().FullName,
                    declaringType,
                    StringComparison.Ordinal) &&
                string.Equals(called.Name, methodName, StringComparison.Ordinal));
    }

    private static void ConvertsMonoAndPreservesWrappedOrder()
    {
        var buffer = new AudioRingBuffer(8);
        Assert(buffer.TryWriteStereo(new[] { 1f, 2f, 3f }, 1), "Initial mono write failed.");

        var first = new float[4];
        AssertEqual(4, buffer.Read(first, first.Length), "Unexpected first read length.");
        AssertSequence(new[] { 1f, 1f, 2f, 2f }, first, "Mono conversion failed.");

        Assert(buffer.TryWriteStereo(new[] { 4f, 40f, 5f, 50f }, 2), "Wrapped stereo write failed.");
        var second = new float[6];
        AssertEqual(6, buffer.Read(second, second.Length), "Unexpected wrapped read length.");
        AssertSequence(new[] { 3f, 3f, 4f, 40f, 5f, 50f }, second, "Wrapped order changed.");
    }

    private static void RejectsOverflowWithoutPartialWrite()
    {
        var buffer = new AudioRingBuffer(4);
        Assert(buffer.TryWriteStereo(new[] { 0.1f, 0.2f }, 1), "Initial write failed.");
        Assert(!buffer.TryWriteStereo(new[] { 0.3f }, 1), "Overflow write should fail.");

        var output = new float[4];
        AssertEqual(4, buffer.Read(output, output.Length), "Overflow changed queued length.");
        AssertSequence(new[] { 0.1f, 0.1f, 0.2f, 0.2f }, output, "Overflow partially mutated the ring.");
    }

    private static void WritesOnlyRequestedSourceSamples()
    {
        var buffer = new AudioRingBuffer(8);
        Assert(
            buffer.TryWriteStereo(new[] { 0.1f, 0.2f, 9f, 9f }, 2, 1),
            "Bounded mono write failed.");

        var output = new float[6];
        AssertEqual(4, buffer.Read(output, output.Length), "Bounded write queued the wrong sample count.");
        AssertSequence(
            new[] { 0.1f, 0.1f, 0.2f, 0.2f, 0f, 0f },
            output,
            "Bounded write consumed samples beyond the callback length.");
    }

    private static void MixesAndClampsConcurrentStreams()
    {
        var mixer = new VoiceAudioMixer();
        VoiceCaptureStream first = mixer.Register(16);
        VoiceCaptureStream second = mixer.Register(16);
        Assert(first.TryWrite(new[] { 0.75f, -0.75f }, 2), "First stream write failed.");
        Assert(second.TryWrite(new[] { 0.75f, -0.75f }, 2), "Second stream write failed.");

        var output = new float[2];
        Assert(mixer.Mix(output, output.Length), "Mixer reported no input.");
        AssertSequence(new[] { 1f, -1f }, output, "Mixer did not clamp the sum.");

        mixer.Unregister(first);
        mixer.Unregister(second);
    }

    private static void ClearDropsQueuedSamples()
    {
        var mixer = new VoiceAudioMixer();
        VoiceCaptureStream stream = mixer.Register(8);
        Assert(stream.TryWrite(new[] { 0.5f }, 1), "Queued write failed.");
        mixer.Clear();

        var output = new float[2];
        Assert(!mixer.Mix(output, output.Length), "Cleared mixer reported queued input.");
        AssertSequence(new[] { 0f, 0f }, output, "Cleared mixer retained samples.");
    }

    private static void ConcurrentClearCannotRestoreReadCursor()
    {
        var buffer = new AudioRingBuffer(8);
        Assert(buffer.TryWriteStereo(new[] { 0.25f, 0.5f }, 1), "Raced write failed.");

        using var copied = new ManualResetEventSlim(false);
        using var resume = new ManualResetEventSlim(false);
        var output = new float[4];
        int read = -1;
        Exception? readerFailure = null;
        var reader = new Thread(
            () =>
            {
                try
                {
                    read = buffer.Read(
                        output,
                        output.Length,
                        () =>
                        {
                            copied.Set();
                            resume.Wait();
                        });
                }
                catch (Exception exception)
                {
                    readerFailure = exception;
                }
            });

        reader.Start();
        try
        {
            Assert(copied.Wait(TimeSpan.FromSeconds(2)), "Reader did not reach the publication barrier.");
            buffer.Clear();
        }
        finally
        {
            resume.Set();
        }

        Assert(reader.Join(TimeSpan.FromSeconds(2)), "Reader did not finish after the clear race.");
        if (readerFailure is not null)
        {
            throw new InvalidOperationException("Raced reader failed.", readerFailure);
        }

        AssertEqual(0, read, "A clear racing publication returned stale samples.");
        AssertEqual(0, buffer.AvailableSamples, "A raced reader restored the pre-clear cursor.");
        AssertSequence(new[] { 0f, 0f, 0f, 0f }, output, "A raced reader exposed stale samples.");

        Assert(buffer.TryWriteStereo(new[] { 0.75f }, 1), "Post-clear write failed.");
        var current = new float[2];
        AssertEqual(2, buffer.Read(current, current.Length), "Post-clear samples were unavailable.");
        AssertSequence(new[] { 0.75f, 0.75f }, current, "Post-clear samples were replaced by stale audio.");
    }

    private static void ReactivationInvalidatesCapturedRegistration()
    {
        var registrations = new AtomicRegistration<object>();
        var original = new object();
        var replacement = new object();
        registrations.Exchange(original);

        using var captured = new ManualResetEventSlim(false);
        using var resume = new ManualResetEventSlim(false);
        bool mayCommit = true;
        var callback = new Thread(
            () =>
            {
                object? registration = registrations.Read();
                captured.Set();
                resume.Wait();
                mayCommit = registration is not null && registrations.IsCurrent(registration);
            });

        callback.Start();
        try
        {
            Assert(captured.Wait(TimeSpan.FromSeconds(2)), "Callback did not capture its registration.");
            registrations.Exchange(null);
            registrations.Exchange(replacement);
        }
        finally
        {
            resume.Set();
        }

        Assert(callback.Join(TimeSpan.FromSeconds(2)), "Callback did not finish after reactivation.");
        Assert(!mayCommit, "A callback from the old activation committed against the replacement.");
        Assert(registrations.IsCurrent(replacement), "Reactivation did not retain the replacement registration.");
    }

    private static void RetirementWaitsForActiveCommit()
    {
        var lease = new AtomicCommitLease();
        Assert(lease.TryBegin(), "Initial callback could not begin its commit.");

        using var retirementStarted = new ManualResetEventSlim(false);
        using var retirementFinished = new ManualResetEventSlim(false);
        var retirement = new Thread(
            () =>
            {
                retirementStarted.Set();
                lease.Retire();
                retirementFinished.Set();
            });

        retirement.Start();
        Assert(retirementStarted.Wait(TimeSpan.FromSeconds(2)), "Retirement did not start.");
        Assert(
            !retirementFinished.Wait(TimeSpan.FromMilliseconds(100)),
            "Retirement completed before the active callback committed.");

        lease.End();
        Assert(retirement.Join(TimeSpan.FromSeconds(2)), "Retirement did not finish after the callback committed.");
        Assert(retirementFinished.IsSet, "Retirement completion was not published.");
        Assert(!lease.TryBegin(), "A retired registration accepted another callback commit.");
    }

    private static void RoutingRetirementWaitsForActiveSubmissions()
    {
        var lease = new AtomicUsageLease();
        Assert(lease.TryBegin(), "First routing submission could not begin.");
        Assert(lease.TryBegin(), "Second routing submission could not begin.");

        using var retirementStarted = new ManualResetEventSlim(false);
        using var retirementFinished = new ManualResetEventSlim(false);
        var retirement = new Thread(
            () =>
            {
                retirementStarted.Set();
                lease.Retire();
                retirementFinished.Set();
            });

        retirement.Start();
        Assert(retirementStarted.Wait(TimeSpan.FromSeconds(2)), "Routing retirement did not start.");
        Assert(
            !retirementFinished.Wait(TimeSpan.FromMilliseconds(100)),
            "Routing retirement completed while submissions were active.");

        lease.End();
        Assert(
            !retirementFinished.Wait(TimeSpan.FromMilliseconds(100)),
            "Routing retirement ignored a remaining submission.");

        lease.End();
        Assert(retirement.Join(TimeSpan.FromSeconds(2)), "Routing retirement did not finish after submissions ended.");
        Assert(!lease.TryBegin(), "A retired routing epoch accepted another submission.");
    }

    private static void RemoteVoiceSelectionCoversSupportedGamePaths()
    {
        AssertRemoteVoiceSelected(
            isLocalPlayer: false,
            isPlayerControlled: true,
            isPlayerDead: false,
            hasVoiceSource: true,
            "Host did not route a living remote client.");
        AssertRemoteVoiceSelected(
            isLocalPlayer: false,
            isPlayerControlled: true,
            isPlayerDead: false,
            hasVoiceSource: true,
            "Client did not route a living remote host.");
        AssertRemoteVoiceSelected(
            isLocalPlayer: false,
            isPlayerControlled: false,
            isPlayerDead: true,
            hasVoiceSource: true,
            "A dead remote player's retained voice source was not routed.");
        AssertRemoteVoiceSelected(
            isLocalPlayer: false,
            isPlayerControlled: true,
            isPlayerDead: false,
            hasVoiceSource: true,
            "A remote player heard while spectating was not routed.");
        AssertRemoteVoiceSelected(
            isLocalPlayer: false,
            isPlayerControlled: true,
            isPlayerDead: false,
            hasVoiceSource: true,
            "The walkie-talkie effect path changed remote-source selection.");

        Assert(
            !RemoteVoiceSelectionPolicy.ShouldCapture(
                isLocalPlayer: true,
                isPlayerControlled: true,
                isPlayerDead: false,
                hasVoiceSource: true),
            "The living local player's microphone playback source was routed.");
        Assert(
            !RemoteVoiceSelectionPolicy.ShouldCapture(
                isLocalPlayer: true,
                isPlayerControlled: false,
                isPlayerDead: true,
                hasVoiceSource: true),
            "The dead local spectator's voice source was routed.");
        Assert(
            !RemoteVoiceSelectionPolicy.ShouldCapture(
                isLocalPlayer: false,
                isPlayerControlled: false,
                isPlayerDead: false,
                hasVoiceSource: true),
            "An unused player slot was routed.");
        Assert(
            !RemoteVoiceSelectionPolicy.ShouldCapture(
                isLocalPlayer: false,
                isPlayerControlled: true,
                isPlayerDead: false,
                hasVoiceSource: false),
            "A player without an assigned playback source was routed.");
    }

    private static void AssertRemoteVoiceSelected(
        bool isLocalPlayer,
        bool isPlayerControlled,
        bool isPlayerDead,
        bool hasVoiceSource,
        string message)
    {
        Assert(
            RemoteVoiceSelectionPolicy.ShouldCapture(
                isLocalPlayer,
                isPlayerControlled,
                isPlayerDead,
                hasVoiceSource),
            message);
    }

    private static void PluginConfigurationDefaultsToSilentFallback(
        string assemblyPath)
    {
        using ModuleDefinition module = ModuleDefinition.ReadModule(assemblyPath);
        TypeDefinition plugin = module.Types.Single(
            type => string.Equals(
                type.FullName,
                "RemoteVoiceSplit.Plugin",
                StringComparison.Ordinal));
        MethodDefinition awake = plugin.Methods.Single(
            method => string.Equals(
                method.Name,
                "Awake",
                StringComparison.Ordinal));
        Instruction bind = awake.Body.Instructions.Single(
            instruction =>
                instruction.Operand is GenericInstanceMethod called &&
                string.Equals(
                    called.DeclaringType.FullName,
                    "BepInEx.Configuration.ConfigFile",
                    StringComparison.Ordinal) &&
                string.Equals(called.Name, "Bind", StringComparison.Ordinal));

        Assert(
            string.Equals(
                bind.Previous?.Previous?.Previous?.Previous?.Operand as string,
                "Audio",
                StringComparison.Ordinal),
            "The fallback setting was not bound in the Audio section.");
        Assert(
            string.Equals(
                bind.Previous?.Previous?.Previous?.Operand as string,
                "FallbackToGameOutput",
                StringComparison.Ordinal),
            "The fallback setting key changed.");
        Assert(
            bind.Previous?.Previous?.OpCode.Code == Code.Ldc_I4_0,
            "The fallback setting did not default to false.");
    }

    private static void VoiceFallbackBoundaryUsesPolicy(string assemblyPath)
    {
        using ModuleDefinition module = ModuleDefinition.ReadModule(assemblyPath);
        TypeDefinition filter = module.Types.Single(
            type => string.Equals(
                type.FullName,
                "RemoteVoiceSplit.Interop.Game.VoiceCaptureFilter",
                StringComparison.Ordinal));
        MethodDefinition callback = filter.Methods.Single(
            method => string.Equals(
                method.Name,
                "OnAudioFilterRead",
                StringComparison.Ordinal));

        Assert(
            Calls(
                callback,
                "RemoteVoiceSplit.Core.RemoteVoiceFallbackPolicy",
                "ShouldClearUnityOutput"),
            "The Unity audio callback did not apply the fallback policy.");
        Assert(
            Calls(
                callback,
                "RemoteVoiceSplit.Core.RemoteVoiceFallbackState",
                "get_FallbackToGameOutput"),
            "The Unity audio callback did not read the live fallback state.");
        Assert(
            Calls(
                callback,
                "RemoteVoiceSplit.Core.VoiceCaptureStream",
                "Clear"),
            "The silent fallback did not discard queued remote voice.");
        Assert(
            Calls(callback, "System.Array", "Clear"),
            "The silent fallback did not clear Unity output.");
    }

    private static void RemoteVoiceFallbackDefaultsToSilence()
    {
        var fallback = new RemoteVoiceFallbackState(
            RemoteVoiceFallbackPolicy.DefaultFallbackToGameOutput);
        Assert(
            !fallback.FallbackToGameOutput,
            "Remote voice fallback did not default to silence.");
        Assert(
            RemoteVoiceFallbackPolicy.ShouldClearUnityOutput(
                submissionAccepted: false,
                fallbackToGameOutput: fallback.FallbackToGameOutput),
            "Unavailable process routing did not silence Unity output by default.");

        fallback.Update(fallbackToGameOutput: true);
        Assert(
            !RemoteVoiceFallbackPolicy.ShouldClearUnityOutput(
                submissionAccepted: false,
                fallbackToGameOutput: fallback.FallbackToGameOutput),
            "The opt-out setting did not preserve Unity output while process routing was unavailable.");
        Assert(
            RemoteVoiceFallbackPolicy.ShouldClearUnityOutput(
                submissionAccepted: true,
                fallbackToGameOutput: fallback.FallbackToGameOutput),
            "Accepted process routing did not clear Unity output when fallback was enabled.");

        fallback.Update(fallbackToGameOutput: false);
        Assert(
            RemoteVoiceFallbackPolicy.ShouldClearUnityOutput(
                submissionAccepted: true,
                fallbackToGameOutput: fallback.FallbackToGameOutput),
            "Accepted process routing did not clear Unity output under the default setting.");
    }

    private static void PluginConfigurationUpdatesFallbackLive(
        string assemblyPath)
    {
        using ModuleDefinition module = ModuleDefinition.ReadModule(assemblyPath);
        TypeDefinition configuration = module.Types.Single(
            type => string.Equals(
                type.FullName,
                "RemoteVoiceSplit.Interop.Game.RemoteVoiceFallbackConfiguration",
                StringComparison.Ordinal));
        MethodDefinition constructor = configuration.Methods.Single(
            method => method.IsConstructor && !method.IsStatic);
        MethodDefinition changed = configuration.Methods.Single(
            method => string.Equals(
                method.Name,
                "OnSettingChanged",
                StringComparison.Ordinal));
        MethodDefinition dispose = configuration.Methods.Single(
            method => string.Equals(
                method.Name,
                "Dispose",
                StringComparison.Ordinal));

        Assert(
            CallsGenericType(
                constructor,
                "BepInEx.Configuration.ConfigEntry`1",
                "add_SettingChanged"),
            "The fallback configuration did not subscribe to live setting changes.");
        Assert(
            Calls(
                changed,
                "RemoteVoiceSplit.Core.RemoteVoiceFallbackState",
                "Update"),
            "A live setting change did not update the audio-thread fallback state.");
        Assert(
            CallsGenericType(
                dispose,
                "BepInEx.Configuration.ConfigEntry`1",
                "remove_SettingChanged"),
            "Fallback configuration shutdown did not unsubscribe its setting handler.");
    }

    private static void RoutingSessionRetiresAndRecovers()
    {
        var gate = new RoutingSessionGate();
        Assert(!gate.IsReady, "A routing session started ready before the host handshake.");
        Assert(
            gate.TryBeginSubmission() is null,
            "An unavailable audio host accepted a submission.");

        gate.Activate();
        Assert(gate.IsReady, "A completed host handshake did not activate routing.");
        RoutingSubmissionLease? active = gate.TryBeginSubmission();
        Assert(active is not null, "An active audio host rejected a submission.");

        using var retirementStarted = new ManualResetEventSlim(false);
        using var retirementFinished = new ManualResetEventSlim(false);
        var hostExit = new Thread(
            () =>
            {
                retirementStarted.Set();
                gate.Deactivate();
                retirementFinished.Set();
            });

        hostExit.Start();
        Assert(retirementStarted.Wait(TimeSpan.FromSeconds(2)), "Host-exit retirement did not start.");
        Assert(
            !retirementFinished.Wait(TimeSpan.FromMilliseconds(100)),
            "Host exit retired routing while an audio callback could still commit.");

        active!.Dispose();
        Assert(hostExit.Join(TimeSpan.FromSeconds(2)), "Host-exit retirement did not finish.");
        Assert(!gate.IsReady, "Host exit left destructive routing enabled.");
        Assert(
            gate.TryBeginSubmission() is null,
            "Host exit continued accepting process-audio submissions.");

        gate.Activate();
        RoutingSubmissionLease? recovered = gate.TryBeginSubmission();
        Assert(recovered is not null, "A replacement audio host did not restore routing.");
        recovered!.Dispose();
        gate.Deactivate();
        Assert(!gate.IsReady, "Final routing retirement did not complete.");
    }

    private static void AudioHostWindowTitleIsStable()
    {
        Assert(
            string.Equals(
                AudioHostWindowIdentity.Title,
                "Lethal Company Remote Voice Split",
                StringComparison.Ordinal),
            "The OBS audio-host window title changed.");
    }

    private static void AudioHostProtocolRoundTrips()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            AudioHostProtocol.WriteClientHello(writer, 48000);
            AudioHostProtocol.WriteServerReady(writer, 1234);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: false);
        AssertEqual(48000, AudioHostProtocol.ReadClientHello(reader), "Protocol changed the sample rate.");
        AssertEqual(1234, AudioHostProtocol.ReadServerReady(reader), "Protocol changed the process ID.");
    }

    private static void ProcessAncestryHandlesTreesAndCycles()
    {
        var tree = new Dictionary<int, int>
        {
            [20] = 10,
            [30] = 20,
            [40] = 10,
            [50] = 60,
            [60] = 50,
        };

        Assert(ProcessAncestry.IsSelfOrDescendant(10, 10, tree), "A process was not its own ancestor.");
        Assert(ProcessAncestry.IsSelfOrDescendant(30, 10, tree), "A grandchild was not recognized.");
        Assert(!ProcessAncestry.IsSelfOrDescendant(40, 20, tree), "A sibling was treated as a descendant.");
        Assert(!ProcessAncestry.IsSelfOrDescendant(50, 10, tree), "A cycle was treated as game ancestry.");
        Assert(!ProcessAncestry.IsSelfOrDescendant(70, 10, tree), "A missing process was treated as a descendant.");
    }

    private static void PcmAudioBufferReadsAndClearsPartialFrames()
    {
        var buffer = new PcmAudioBuffer();
        buffer.Write(new[] { 0.25f, -0.25f, 0.5f, -0.5f }, 4);

        var first = new float[6];
        Assert(buffer.Read(first, first.Length), "Host buffer did not report queued audio.");
        AssertSequence(
            new[] { 0.25f, -0.25f, 0.5f, -0.5f, 0f, 0f },
            first,
            "Host buffer did not clear the unfilled render tail.");

        var second = new float[2];
        Assert(!buffer.Read(second, second.Length), "Host buffer repeated consumed samples.");
        AssertSequence(new[] { 0f, 0f }, second, "Empty host buffer did not render silence.");
    }

    private static void DefaultEndpointChangeRetiresAndRecovers()
    {
        var gate = new RoutingSessionGate();
        gate.Activate();
        using var endpointFailure = new ManualResetEventSlim(false);
        Exception? observedFailure = null;
        var buffer = new PcmAudioBuffer();
        using (var pump = new AudioOutputPump(
                   buffer,
                   48000,
                   exception =>
                   {
                       observedFailure = exception;
                       gate.Deactivate();
                       endpointFailure.Set();
                   },
                   () => "RemoteVoiceSplit-forced-endpoint-change"))
        {
            pump.WaitUntilStarted(TimeSpan.FromSeconds(5));
            Assert(
                endpointFailure.Wait(TimeSpan.FromSeconds(6)),
                "A changed default multimedia endpoint did not stop the audio session.");
        }

        Assert(
            observedFailure is InvalidOperationException &&
            observedFailure.Message.Contains("default endpoint changed", StringComparison.Ordinal),
            "The endpoint-change failure reason was not preserved.");
        Assert(!gate.IsReady, "Endpoint invalidation left destructive routing enabled.");
        Assert(
            gate.TryBeginSubmission() is null,
            "Endpoint invalidation did not fail open to Unity playback.");

        gate.Activate();
        using (var recoveryPump = new AudioOutputPump(
                   new PcmAudioBuffer(),
                   48000,
                   exception => observedFailure = exception,
                   AudioEndpointService.GetDefaultRenderEndpointId))
        {
            recoveryPump.WaitUntilStarted(TimeSpan.FromSeconds(5));
            using RoutingSubmissionLease? recovered = gate.TryBeginSubmission();
            Assert(recovered is not null, "The current default endpoint did not restore routing.");
        }

        gate.Deactivate();
    }

    private static void AudioHostReconnectCrashAndRestart(
        string audioHostPath,
        TimeSpan soakDuration)
    {
        using (LiveAudioHostConnection disconnected = StartAudioHost(audioHostPath))
        {
            AssertHostSurvives(disconnected.Process, soakDuration);
            string pipeName = disconnected.PipeName;
            int persistentProcessId = disconnected.Process.Id;
            disconnected.CloseClient();
            AssertHostSurvives(
                disconnected.Process,
                TimeSpan.FromSeconds(17));

            using LiveAudioHostConnection reconnected = ConnectAudioHost(
                pipeName,
                persistentProcessId);
            AssertEqual(
                persistentProcessId,
                reconnected.Process.Id,
                "Pipe recovery replaced the stable OBS audio-host process.");
        }

        using (LiveAudioHostConnection crashed = StartAudioHost(audioHostPath))
        {
            crashed.Process.Kill();
            Assert(crashed.Process.WaitForExit(5000), "The killed audio host did not terminate.");
            AssertEqual(-1, crashed.Pipe.ReadByte(), "The client pipe remained readable after the host crash.");
        }

        using (LiveAudioHostConnection recovered = StartAudioHost(audioHostPath))
        {
            recovered.CloseClient();
            Thread.Sleep(500);
            Assert(
                !recovered.Process.HasExited,
                "The replacement audio host did not retain its reconnectable session.");
        }
    }

    private static void AssertHostSurvives(Process process, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < duration)
        {
            Assert(
                !process.HasExited,
                $"The audio host exited during a {duration.TotalSeconds:0}-second connected soak.");
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }

    private static LiveAudioHostConnection StartAudioHost(string audioHostPath)
    {
        string pipeName = $"RemoteVoiceSplit-{Environment.ProcessId}-{Guid.NewGuid():N}";
        int launchedProcessId = DetachedAudioHostLauncher.Launch(
            audioHostPath,
            pipeName,
            Environment.ProcessId);
        return ConnectAudioHost(pipeName, launchedProcessId);
    }

    private static LiveAudioHostConnection ConnectAudioHost(
        string pipeName,
        int expectedProcessId)
    {
        Process? process = null;
        var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.None);

        try
        {
            pipe.Connect(timeout: 5000);
            var reader = new BinaryReader(pipe, System.Text.Encoding.UTF8, leaveOpen: true);
            var writer = new BinaryWriter(pipe, System.Text.Encoding.UTF8, leaveOpen: true);
            AudioHostProtocol.WriteClientHello(writer, 48000);
            int reportedProcessId = AudioHostProtocol.ReadServerReady(reader);
            AssertEqual(
                expectedProcessId,
                reportedProcessId,
                "The audio host handshake reported another process.");
            process = Process.GetProcessById(reportedProcessId);
            Assert(
                !ProcessTreeSnapshot.IsSelfOrDescendant(process.Id, Environment.ProcessId),
                "The production launcher left the audio host inside the test process tree.");
            string windowTitle = string.Empty;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                process.Refresh();
                windowTitle = process.MainWindowTitle;
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    break;
                }

                Thread.Sleep(50);
            }

            Assert(
                string.Equals(
                    AudioHostWindowIdentity.Title,
                    windowTitle,
                    StringComparison.Ordinal),
                $"The live audio-host window title was '{windowTitle}'.");
            return new LiveAudioHostConnection(
                pipeName,
                process,
                pipe,
                reader,
                writer);
        }
        catch
        {
            pipe.Dispose();
            process ??= TryGetProcess(expectedProcessId);
            if (process is not null)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }

                process.Dispose();
            }

            throw;
        }
    }

    private static Process? TryGetProcess(int processId)
    {
        try
        {
            return Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
        }
    }

    private static void AssertSequence(float[] expected, float[] actual, string message)
    {
        if (expected.Length != actual.Length)
        {
            throw new InvalidOperationException($"{message} Length mismatch.");
        }

        for (int index = 0; index < expected.Length; index++)
        {
            if (Math.Abs(expected[index] - actual[index]) > 0.00001f)
            {
                throw new InvalidOperationException($"{message} Index {index}: expected {expected[index]}, got {actual[index]}.");
            }
        }
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private sealed class LiveAudioHostConnection : IDisposable
    {
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;
        private bool _clientClosed;

        public LiveAudioHostConnection(
            string pipeName,
            Process process,
            NamedPipeClientStream pipe,
            BinaryReader reader,
            BinaryWriter writer)
        {
            PipeName = pipeName;
            Process = process;
            Pipe = pipe;
            _reader = reader;
            _writer = writer;
        }

        public Process Process { get; }

        public string PipeName { get; }

        public NamedPipeClientStream Pipe { get; }

        public void CloseClient()
        {
            if (_clientClosed)
            {
                return;
            }

            _clientClosed = true;
            TryDisposePipeComponent(_writer);
            TryDisposePipeComponent(_reader);
            TryDisposePipeComponent(Pipe);
        }

        public void Dispose()
        {
            CloseClient();
            if (!Process.HasExited)
            {
                Process.Kill();
                Process.WaitForExit(5000);
            }

            Process.Dispose();
        }

        private static void TryDisposePipeComponent(IDisposable component)
        {
            try
            {
                component.Dispose();
            }
            catch (IOException)
            {
                // A killed or failed host is expected to leave a broken pipe.
            }
        }
    }
}
