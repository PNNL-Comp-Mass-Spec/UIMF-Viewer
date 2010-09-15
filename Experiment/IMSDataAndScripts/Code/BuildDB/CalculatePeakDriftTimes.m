%%Function used to detect peaks in a histogram, mostly for the IMS
%%dimension but can be used in generic fashion.


%%The algorithm operates on a basic set of assumptions to classify a point
%%as a peak
%%Assumption 1: The peak should have at least 2 points before and 2 points
%%after with lower intensities, climbing up a slope and going down
%%Assumption 2: Peak intensity should be greater than a globally defined
%%cut off for a particular dataset. Another routine determines this cut off
%%Assumption 3: Variation in intensity from peak top to peak bottom should
%%be greater than 50%

%%Return - A list of peak drift times in the input array. 
%%Comments - This needs to be modified to return more information that will
%%eventually end up in the AMT-IMS database. We also need to determine the
%%start of the peak and the end of the peak along with the position of the
%%max peak. 

function [out_peak_drift_times]=CalculatePeakDriftTimes(obs_drift_times, obs_intensity_values, intensityCutoff)

%%this is based on the pulse rate of the dataset, Should be calculated
%%directly from that pulse rate.. for the IMS dimension
driftDifferenceCufoff = 0.18;


%sort the drift times and the corresponding intensity values
[sorted_drift_times, index] = sort(obs_drift_times);
y = obs_intensity_values(index);

%%calculate the continuity peak difference in terms of drift times.
%%if the measurements are apart more than one pulse then we can make a
%%guess as to whether it's a disjoint signal
distance = diff(sorted_drift_times);

%compute a three-point moving average for smoothing the intensity values
intensity_array=MovingAverage(y, 3);

i = 1;
[row, col] = size(intensity_array);
out_peak_drift_times = [];
all_temp_peaks = [];
peak_starts = [];
peak_ends = [];

%%variable to keep track of the start index for a peak
peakStartIndex = i;
%%boolean to keep track of whether the drift times are far apart...
existingPeak = 1;
numPointsGoingUp = 0;
numPointsGoingDown = 0;

%%put a try catch in place for any points towards the end of the array that
%%start getting consideration for peaks etc.
try
while (i < row)
    if existingPeak == 0
        %%starting a new peak...
        
        %%skip to the next closest drift point for consideration
        %%or keep traveling ahead in lieu of noise peaks

        while (distance(i) > driftDifferenceCufoff) || (intensity_array(i) < intensityCutoff)
                i = i + 1;
        end;

        existingPeak = 1;
        peakStartIndex = i;
        numPointsGoingUp = 0;
    end;
    
    %%start climbing up a peak
    while ( i < row-1 && intensity_array(i+1) > intensity_array(i))
        i = i + 1;
        numPointsGoingUp = numPointsGoingUp + 1;
    end;
    
    tempPeakDrift = sorted_drift_times(i);
    tempPeakIndex = i;
    tempPeakAmp = intensity_array(i);
    %%maintain the temporary peak vector with peak drift time, amplitude
    %%and index
    all_temp_peaks = [all_temp_peaks; tempPeakDrift tempPeakAmp i];    

    %%have to check all the way till the end
    %%if we have a bottom reached
    numPointsGoingDown = 0;
    while ( i <= row-1 && intensity_array(i+1)< intensity_array(i))
        i=i+1;
        numPointsGoingDown = numPointsGoingDown + 1;
    end;
    
    bottomAmp = intensity_array(i);
    bottomPeakIndex = i;
    ratio = bottomAmp/tempPeakAmp;
    
    if ( (ratio < 0.5)  && (tempPeakAmp > intensityCutoff) && (numPointsGoingUp >= 2) && (numPointsGoingDown >= 2)) 
        peakEndIndex = bottomPeakIndex;         
        out_peak_drift_times = [out_peak_drift_times tempPeakDrift ];
        peak_starts = [peak_starts; sorted_drift_times(peakStartIndex)];
        peak_ends = [peak_ends; sorted_drift_times(peakEndIndex)];
        peakStartIndex = i + 1 ;
        existingPeak = 0;
    else
        %%we are discarding this point from being a valid peak.
        %%So now check if the next point is within the acceptable drift
        %%time difference window, if that's the case then we continue the
        %%peak else reset the starting point of this peak
        if (distance(i) > driftDifferenceCufoff)
            existingPeak = 0;
        end;
    end;
    
    i = i + 1;
   
end;
catch
end;


end