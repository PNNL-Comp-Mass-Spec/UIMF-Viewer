function [scanNumber]= getScanNumberForMaxPeak(peakList)
    [C I] = max(peakList(:,4));
    scanNumber = peakList(I,1);
