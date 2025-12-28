import wave
import math
import struct
import random
import os

# Configuration
SAMPLE_RATE = 44100
AMPLITUDE = 32767 // 2  # Max volume

def write_wav(filename, samples):
    with wave.open(filename, 'w') as wav_file:
        # Set parameters: 1 channel (mono), 2 bytes size, 44100 Hz rate
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(SAMPLE_RATE)
        
        # Convert samples to binary data
        data = bytearray()
        for sample in samples:
            # Clamp values to prevent distortion
            val = max(-32767, min(32767, int(sample)))
            data.extend(struct.pack('<h', val))
            
        wav_file.writeframes(data)
    print(f"Generated: {filename}")

def generate_tone(frequency, duration, decay=True):
    samples = []
    num_samples = int(duration * SAMPLE_RATE)
    for i in range(num_samples):
        t = float(i) / SAMPLE_RATE
        val = AMPLITUDE * math.sin(2.0 * math.pi * frequency * t)
        
        if decay:
            # Linear decay to 0
            val *= (1.0 - (float(i) / num_samples))
            
        samples.append(val)
    return samples

def generate_noise(duration, decay=True):
    samples = []
    num_samples = int(duration * SAMPLE_RATE)
    for i in range(num_samples):
        val = random.uniform(-AMPLITUDE, AMPLITUDE)
        if decay:
             val *= (1.0 - (float(i) / num_samples))
        samples.append(val)
    return samples

def generate_slide(start_freq, end_freq, duration):
    samples = []
    num_samples = int(duration * SAMPLE_RATE)
    for i in range(num_samples):
        t = float(i) / SAMPLE_RATE
        # Linear interpolation of frequency
        current_freq = start_freq + (end_freq - start_freq) * (i / num_samples)
        val = AMPLITUDE * math.sin(2.0 * math.pi * current_freq * t)
        # Apply slight volume envelope
        vol = 1.0 - (i / num_samples)
        samples.append(val * vol)
    return samples

def main():
    if not os.path.exists("PlaceholderSounds"):
        os.makedirs("PlaceholderSounds")

    # 1. UI Click: High, short blip
    click_sfx = generate_tone(800, 0.1)
    write_wav("PlaceholderSounds/UI_Click.wav", click_sfx)

    # 2. Combat Start: Low "Gong" sound (Mixed frequencies)
    gong_sfx = [a + b for a, b in zip(generate_tone(110, 1.5), generate_tone(220, 1.5))]
    write_wav("PlaceholderSounds/Combat_Start.wav", gong_sfx)

    # 3. Attack: White noise "Swoosh"
    attack_sfx = generate_noise(0.2)
    write_wav("PlaceholderSounds/Attack_Swing.wav", attack_sfx)

    # 4. Hit Impact: Low thud
    hit_sfx = generate_tone(80, 0.15)
    write_wav("PlaceholderSounds/Hit_Impact.wav", hit_sfx)

    # 5. Death: Sliding down tone
    death_sfx = generate_slide(400, 50, 0.5)
    write_wav("PlaceholderSounds/Unit_Death.wav", death_sfx)

    # 6. Victory: Major Triad Arpeggio (C - E - G)
    note_c = generate_tone(523.25, 0.15)
    note_e = generate_tone(659.25, 0.15)
    note_g = generate_tone(783.99, 0.4)
    victory_sfx = note_c + note_e + note_g
    write_wav("PlaceholderSounds/Round_Victory.wav", victory_sfx)

    # 7. Defeat: Discordant Descending (F# - C)
    bad_1 = generate_tone(369.99, 0.2) # F#
    bad_2 = generate_tone(261.63, 0.6) # C
    defeat_sfx = bad_1 + bad_2
    write_wav("PlaceholderSounds/Round_Defeat.wav", defeat_sfx)

    print("\nDone! Drag the 'PlaceholderSounds' folder into Unity.")

if __name__ == "__main__":
    main()