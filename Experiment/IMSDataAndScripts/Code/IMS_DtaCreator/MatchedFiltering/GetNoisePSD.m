function out = GetNoisePSD (one_channel, snr_X)

I = find (one_channel <= snr_X);
noise = one_channel (I);
if max(noise) > 0
    noiseW = fft(noise);
    noiseWConjugate = conj (noiseW);
    noisePSD = (noiseW * noiseWConjugate')/(2*pi);
else
    noisePSD = 1;
end
out = noisePSD;