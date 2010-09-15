function out = BuildMatchFilter(data_channel, snr,  sigma, shift)

% Get the noise out first
I = find (data_channel <= snr);
noise = data_channel (I);
noiseW = fft(noise);
noiseWConjugate = conj (noiseW);
noisePSD = (noiseW * noiseWConjugate')/(2*pi);

%Build X

K = 1;

gauss_dimension = ceil((4*sigma));
A = 1.60/gauss_dimension;
m = gauss_dimension/2;
for j = shift+1:gauss_dimension+shift+1
      gauss(j-shift) = A *exp(-((j-shift-m)^2)/(2*sigma^2));
end


if (noisePSD ==0)
    noisePSD = 1;
end
h = ifft(conj(fft(gauss))/abs(noisePSD));
out = h;