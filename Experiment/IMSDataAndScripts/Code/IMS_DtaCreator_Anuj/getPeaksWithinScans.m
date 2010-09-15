%%This function takes a list of peaks from the parent spectra and finds
%%the peaks that fall within a particular scan range.

%%generally the scan range is of the fragment spectra

%%returns a list of peaks that are inside that scan range


function [peaks] = getPeaksWithinScans(peakList, startscan, endscan)
    indices = peakList(:,1) >= startscan & peakList(:,1) <= endscan;
    peaks = peakList(indices,:);

