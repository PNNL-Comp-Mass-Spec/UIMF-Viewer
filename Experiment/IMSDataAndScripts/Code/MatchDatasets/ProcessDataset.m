% Function to process a single dataset
% Input  is just the fileName which is _drifts file to be matched to the database
%  Operations performed 
%       - Look for isos file and then perform UMC finding
%       - Allocates drift time to each UMC  
%       - Calls PkMatching     
%      
%**************************************************************************
%**************************************************************************
% Initialize
function ProcessDataset(fileName);


%% Note while doing UMC finding using LCMSfeatureFinder.exe there may be
%% variation from the number of UMCs reported by VIPER and reported here.
%% The sources of variation are
%%      - minIsotopicFit - tpically set to 0.15 in VIPER 
%%      - maxAbundance - ignored here due to computational requirements
%%      - Tolerance Settings - default params are used here, if you need to
%%      change, copy the Tmp_Export_LCMSFeaturesToSearch.ini from VIPER
%%      installation folder (this should contain your most recent params)
%%      to the same directory as the data and rename it to correspond to the
%%      - Cleaning up settings - setting in VIPER is to remove scans less
%%      than 2 and greater than 400. The commensurate parameters here are
%%      minScanCount and maxScanCount. Other cleaning-up settings are not
%%      used here.

HydrogenMass = 1.00794;
%% params
numCols = 13 ; 
minIsotopicFit = 0.15  ; 
minScanCount = 3 ; 
maxScanCount = 400 ; 
minAbundance = 700;
maxAbundance = 1e+15;

TIndex = findstr(fileName, '_all_drifts.csv') ; 
datasetName = fileName(1:TIndex) ; 
isosName = strcat(datasetName, 'all_isos.csv') ; 
filteredIsosName = strcat(datasetName, 'filtered_isos.csv');
viperInput = 1;

if (viperInput == 0)

    %% Now call LCMSFeatureFinder
    display('Finding UMCs') ; 
    
    %%first lets do some filtering on the isos file ...
%     isosData = csvread(isosName, 1, 0) ;
%     filteredIndices = isosData(:,6) > minAbundance & isosData(:,6)< maxAbundance;
%     filteredData = isosData(filteredIndices,:);
%     filteredIndices = filteredData(:,11) > minIsotopicFit;
%     filteredData = filteredData(filteredIndices, :);
%     csvwrite (filteredIsosName, filteredData);
    
    strExe = strcat('C:\DriftTimePrediction\Code\MatchDatasets\LCMSFeatureFinder.exe -I:', filteredIsosName) ; 
    system(strExe) ;

    % Now load in the features and pk-feature map
    featuresName = strcat(datasetName, 'all_isos_Features.txt') ; 
    %%read from row 1 as first row is headers
    featuresData = dlmread(featuresName, '\t', 1, 0) ; 
    pkFeaturesMapName = strcat(datasetName, 'all_isos_PeakToFeatureMap.txt') ; 
    
    %%read from row 1 as first row is headers
    pkFeatureMap = dlmread(pkFeaturesMapName, '\t', 1, 0) ; 
    numFeatures = size(featuresData, 1) ;

    if (minIsotopicFit < 1)
        %% Read in isos
        display('Reading in Isos') ; 
        isosData = csvread(isosName, 1, 0) ;
    
        % Filter based on min_isotopic_fit
        display('Filtering based on Fit') ; 
    
        indexFeaturesPassingFitTest = find(isosData(:,5) < minIsotopicFit) ; 
        %%ARS - changing this to greater than sign 
        %%the fit needs to be better than a certain number
        indexFeaturesPassingFitTest = find(isosData(:,5) > minIsotopicFit) ; 
       
        umcsToKeep = []; 
        for i = 1: size(indexFeaturesPassingFitTest, 1)
            indexInMap = find(pkFeatureMap(:,2) == indexFeaturesPassingFitTest(i)) ; 
            umcsToKeep = [umcsToKeep pkFeatureMap(indexInMap, 1)] ; 
        end
        uniqueUmcs = unique(umcsToKeep');
        filteredFeaturesData_1 = featuresData(uniqueUmcs+1, :) ; % the plus one to account for MATLAB indexing
    else
        filteredFeaturesData_1 = featuresData ; 
    end


    % Filter based on scan count
    display('Filtering based on UMC size') ; 
    start_scan_col = 3 ; 
    end_scan_col = 4 ; 
    numScans = filteredFeaturesData_1(:, end_scan_col) - filteredFeaturesData_1(:, start_scan_col) ; 
    indexUMCsToKeep = find(numScans >= minScanCount-1 & numScans < maxScanCount) ; 
    numFilteredFeatures = size(indexUMCsToKeep,1) ; 
    filteredFeaturesData = filteredFeaturesData_1(indexUMCsToKeep, :) ; 
    
    clear featuresData filteredFeaturesData_1 pkFeaturesMap isosData; 
    
    features_index_col = 1; 
    features_rep_scan_col = 2 ; 
    features_rep_mass_col = 7 ; 
    features_intensity_col = 9 ; 
    features_mz_col = 10 ; 
    features_rep_cs_col =11;


else
    %% Jun 10 modification - simply reading in the VIPER output
     features_index_col = 1; 
     features_rep_scan_col = 10 ; 
     features_rep_cs_col =11;
     features_rep_mass_col = 12; 
     features_mz_col = 13 ; 
     features_intensity_col = 14 ; 
     fprintf ('Processing file %s', datasetName)
     umcFileName = strcat(datasetName, 'UMC_List.csv') ;
     filteredFeaturesData = csvread(umcFileName, 1, 0) ; 
     numFilteredFeatures = size(filteredFeaturesData, 1) ; 

end

sprintf('Number of UMCs found = %d', numFilteredFeatures)
% Get out drift information
dataDrifts = []; 
display('Loading previously saved drifts data information ') ; 
lc_ims_data = csvread(fileName, 1, 0 ) ;

lc_scan_column = 1 ; 
drift_time_column = 3 ; 
cs_column = 5;
abundance_column = 6 ; 
mono_mass_column = 8 ; 
pressure_column = 11 ; 
ppm_tolerance = 20 ; 
standard_pressure = 4 ; 
resolvedFeaturesData =[] ; 
numResolvedFeatures = 1 ; 

%driftIntensityCutoff = FindNoiseLevelCutoff(lc_ims_data(:,abundance_column), 2);

display('Acquiring Drift Time Information') ;
for i = 1:numFilteredFeatures
    ims_data = [] ; 
    this_umc_index = filteredFeaturesData(i, features_index_col) ; 
    this_umc_rep_scan = filteredFeaturesData(i, features_rep_scan_col) ;  
    this_umc_rep_mass = filteredFeaturesData(i, features_rep_mass_col) ;
    this_umc_rep_cs = filteredFeaturesData(i, features_rep_cs_col);
    index_lc_scan = find(lc_ims_data(:, lc_scan_column) == this_umc_rep_scan & lc_ims_data(:, cs_column)== this_umc_rep_cs);
    ims_data = lc_ims_data(index_lc_scan, :) ;
    
    % find the entry that has same mass/charge as the matched_mass/charge
    this_umc_rep_mz = (this_umc_rep_mass + (HydrogenMass*this_umc_rep_cs))/this_umc_rep_cs;
    
    ppm = abs( ((HydrogenMass*this_umc_rep_cs)+ims_data(:, mono_mass_column))./ims_data(:,cs_column) - this_umc_rep_mz)/this_umc_rep_mz ; 
    ppm = ppm * 1000000 ;
    index_mz = find(ppm < ppm_tolerance);
    
    if (size(index_mz, 1) > 0)
        %located! get drift_times out
        pressure = ims_data(index_mz(1), pressure_column) ;
        drift_times = ims_data(index_mz, drift_time_column)  ;
        
        % correct for pressure
        if (pressure == 0)
            pressure = 4 ; 
        end
             
        drift_times = drift_times .* (standard_pressure./pressure);
        intensity_values = ims_data(index_mz, abundance_column) ;        
        
%         if ( size(drift_times,1) > 4 )
%             save(['driftTime' num2str(i) '.mat'], 'drift_times', 'intensity_values');
%         end
        % resolve them into conformers if any
%         [resolved_id resolved_drift_times]= CalculatePeakDriftTimesOld(drift_times, intensity_values, this_umc_index) ;
        resolved_drift_times = CalculatePeakDriftTimes(drift_times, intensity_values, 420.00) ;
        
        % now add them to dataDrifts
        if size(resolved_drift_times, 1) > 0
            for j = 1:size(resolved_drift_times,1)
                dataDrifts = [dataDrifts ; this_umc_index j resolved_drift_times(j) this_umc_rep_cs] ; 
            end
            numResolvedFeatures = numResolvedFeatures + 1 ; 
        else
            filteredFeaturesData(i,1) = -1;
        end;
    else
        % couldn't find the entry so mark the umc to be removed from consideration
        filteredFeaturesData(i,1) = -1 ; 
    end
end

% remove all neg entry umcs
newFilteredFeaturesData = filteredFeaturesData(find(filteredFeaturesData(:, features_index_col)> -1), :) ; 
clear filteredFeaturesData ; 
numNewFilteredFeatures = size(newFilteredFeaturesData, 1) ; 

% Now start building the matrix to give to PkMatching
dataLCMS = zeros(numNewFilteredFeatures, 8) ; 

mz_col = 1 ;
mass_col = 2 ;
scan_col = 3 ;
intensity_col = 4 ;
pair_col = 5 ;
tag_id_col = 6 ;
umc_col = 7 ;
cs_col = 8;

dataLCMS(:, [mz_col mass_col scan_col]) =[newFilteredFeaturesData(:, features_mz_col) newFilteredFeaturesData(:, features_rep_mass_col) newFilteredFeaturesData(:, features_rep_scan_col)] ; 
dataLCMS(:, [intensity_col]) = newFilteredFeaturesData(:, features_intensity_col) ; 
dataLCMS(:, umc_col) = [1:numNewFilteredFeatures] ; 
dataLCMS(:, cs_col) = newFilteredFeaturesData(:, features_rep_cs_col); 

save 'MaxPeakSessionVariables.mat';

% 
% % Now call PkMatching for drift time data
PkMatchDataset(dataLCMS, dataDrifts) ; 

end