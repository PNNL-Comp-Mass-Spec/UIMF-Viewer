function cutoff = FindNoiseLevelCutoff(intensities, iterations)

%make a copy of the intensity matrix
amp = intensities;

for i = 0:iterations-1
    %%calculate mean and std deviation of the copy
    stdDeviation = std(amp);
    average = mean(amp);
    
    %%find all values that are below the mean + 5 stddeviation cut off
    B = find(intensities < (average + 5*stdDeviation));
    
    amp = intensities(B);
end

cutoff = mean(amp);



