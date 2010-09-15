% Function to match a LC-IMS-MS dataset to a target DB
%
% Two files are the main input
%               - dLCMS : matirx of UMC features with LC-MS information
%               - dDrifts : matrix of UMC features with drifts information
%
% Output is a an excel file containing output results and a bunch of images
% depicting mass error, alignment and such (similar to VIPER)
%**************************************************************************
%**************************************************************************

function PkMatchDataset(dLCMS,dDrifts);

global DBMassTags ;
%global dLCMS ;
%global dDrifts ;
%load data_modified_features
%load data_1800_alternate


% init stuff
standard_pressure = 4 ;
h = actxserver('MassMatchCOM.MassMatchWrapper') ;

dbMassTagsMain = DBMassTags{1} ;
dbMassTagInfo = dbMassTagsMain(:, [1:5]) ;
dbPepList = DBMassTags{2} ;
%dbProteinList = DBMassTags{3} ;

%db Cols
dbNetCol =  1 ;
dbMassCol = 2 ;
dbNumObservationsCol = 3 ;
dbIdCol = 4 ;
dbXcorrCol = 5 ;
dbDtCol = 6 ;
dbCsCol = 7;
dbConfId = 8;

%UMC cols
mz_col = 1 ;
mass_col = 2 ;
scan_col = 3 ;
intensity_col = 4 ;
pair_col = 5 ;
tag_id_col = 6 ;
umc_col = 7 ;

% For LCMSWarp
%NET options
num_ms_sections = 100 ;
contraction_factor = 3 ;
max_discontinuity = 10 ;
max_promiscuity = 2 ;
%mass options
mass_window_for_cal = 50 ;
mass_jump = 50 ;
num_mass_delta_bins = 100 ;
num_slices_for_mass_cal = 20 ;
%tolerances
warp_mass_tolerance = 25;
warp_net_tolerance = 0.025 ;
%calibration
recalibration_type = 2 ; %mass correction: 0 is mz based, 1 is scan based and 2 is hybrid
%advanced
regression_order = 2 ;
use_lsq = 1 ;
ztolerance = 3 ;
%plots
min_net = -1 ;
max_net = 1 ;
%alignment
alignment_type = 1 ; % 0 is for mswarp on net only, 1 is mswarp on mass and net.

% Misc.
numFeatures = size(dLCMS,1) ;
numMassTags = size(dbMassTagInfo) ;

h.SetNetOptions(int32(num_ms_sections), int16(contraction_factor), int16(max_discontinuity), warp_net_tolerance, min_net, max_net, int32(max_promiscuity)) ;
h.SetRegressionOrder(int16(regression_order)) ;
h.SetRecalibrationType(int16(recalibration_type)) ;
h.SetAlignmentType(int16(alignment_type)) ;
h.SetMassOptions(warp_mass_tolerance, num_mass_delta_bins, mass_window_for_cal, mass_jump, num_slices_for_mass_cal, ztolerance, use_lsq);

% % % Choosing intensities - a rather haphazard way, basically just keeping all
% % % intensity value greater than 0.25 quantile
% % display('Removing noise from data') ; 
% % dataIntensities = dLCMS(:, intensity_col) ; 
% % intensityLowerQuantile  = quantile(dataIntensities, 0.25) ; 
% % intensityUpperQuantile = quantile(dataIntensities, 0.75) ; 
% % realIntensityIndex = find(dataIntensities > intensityLowerQuantile & dataIntensities < intensityUpperQuantile) ; 
% % newdLCMS = dLCMS(realIntensityIndex, :) ; 
% % newdDrifts = dDrifts(realIntensityIndex, :) ; 
% % numFeatures = size(newdLCMS) ; 
% % dDrifts = newdDrifts ; 
% % dDrifts(:, 1) = [1:numFeatures] ; 
% % dLCMS = newdLCMS ; 
% % dLCMS(:, umc_col) = [1:numFeatures] ; 


% Align with msFeatures
display('Performing LC-MS Warp') ;
[MatchScores,AlignmentFunc,Matches,PepTransformedRT,TransformedRT,MassErrorHist,NetErrorHist] = h.MS2MSMSDBAlignPeptides(dLCMS, dbMassTagInfo) ;
% cols for Matches
% 1:umc_index 2:mtid 3:net_error 4:mass_error
mswarp_identified_mtids = Matches(:, 2) ;
num_umcs_identified = size(mswarp_identified_mtids, 1) ;

% Regress
% display('Performing Drift Time Regression') ;
confirmed_drifts = [] ;
observed_drifts = [];
drift_umc_col = 1 ;
drift_confidx_col = 2;
drift_col = 3;  % ignoring conformer info in col 2 for the time being
for i = 1:num_umcs_identified
    Iindex = find(dbMassTagsMain(:, dbIdCol) == mswarp_identified_mtids(i) ) ;
    %for j = 1: size(Iindex,1)
        confirmed_drifts = [confirmed_drifts; [mswarp_identified_mtids(i) dbMassTagsMain(Iindex(1), dbConfId) dbMassTagsMain(Iindex(1), dbDtCol) dbMassTagsMain(Iindex(1), dbCsCol)]] ;
   % end;
end
observed_drifts = dDrifts(Matches(:,1), :);
save ('Regression.mat', 'confirmed_drifts', 'observed_drifts');
% Calibrate
display('Performing mass, net calibration') ;
calibratedUMCFeatures = [] ;
max_mass_shift_ppm = 10000;
 

for featureNum = 1:numFeatures
    massShiftPPM = -1 *h.GetPPMShift(dLCMS(featureNum, mz_col), dLCMS(featureNum, scan_col));
    massShiftDa = massShiftPPM/1000000 * dLCMS(featureNum, mass_col) ;
    if (massShiftPPM < max_mass_shift_ppm)
        calibratedMass = dLCMS(featureNum, mass_col) + massShiftDa ;
        indexFeature = find(PepTransformedRT(:,1) == featureNum) ; 
        calibratedNET  = PepTransformedRT(indexFeature, 2) ;
        umcIntensity = dLCMS(featureNum, intensity_col) ; 
        calibratedUMCFeatures = [calibratedUMCFeatures ; featureNum calibratedMass calibratedNET dDrifts(featureNum, 3) umcIntensity] ;
    end
end

% Search
display('Searching DB using mass/net tolerances, DT tolerance is 100') ;
display('***************************************************************');
search_mass_tol  = 20 ;
search_net_tol = 0.025 ;
search_dt_tol = 100 ;
dbMTMassNetDt = [dbMassTagInfo(:, dbIdCol) dbMassTagInfo(:, dbMassCol) dbMassTagInfo(:, dbNetCol) dbMassTagsMain(:, dbDtCol)] ;
[TrueMatches] = SearchForMTIDs(dbMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
num_identified_mtids = size(unique(TrueMatches(:,2)), 1) ;
sprintf('Identifications using Mass & NET =  %d', num_identified_mtids)

% FDR search
display('Performing 11Da shift and searching') ;
shiftedMTMassNetDt = [dbMassTagInfo(:, dbIdCol) dbMassTagInfo(:, dbMassCol)+11 dbMassTagInfo(:, dbNetCol) dbMassTagsMain(:, dbDtCol)] ;
[FalseMatches] = SearchForMTIDs(shiftedMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
num_false = size(unique(FalseMatches(:,2)), 1) ;
sprintf('Identifications using Mass+ 11Da & NET =  %d',num_false)
display('***************************************************************');

display('Performing tolerance refinement using EM algorithm')
mass_err = TrueMatches(:, 3)';
net_err = TrueMatches(:, 4)' ; 
dt_err = TrueMatches(:, 5)' ; 
[sigma_mass mu_mass] = EM_1D(mass_err,0) ; 
[sigma_net mu_net] = EM_1D(net_err,0) ; 
%[sigma_dt mu_dt] = EM_1D(dt_err,0) ;

search_mass_tol = max(abs([mu_mass-2*sigma_mass mu_mass+2*sigma_mass]))  
search_net_tol = max(abs([mu_net-2*sigma_net mu_net+2*sigma_net]))  
%search_dt_tol = max(abs([mu_dt-2*sigma_dt mu_dt+2*sigma_dt])) ;
%search_mass_tol = 10;
%search_net_tol = 0.01;
%search_dt_tol = 1;
[TolMatches] = SearchForMTIDs(dbMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, 100) ;
sprintf('Identifications with Tolerance refinement on Mass & NET =  %d',length(unique(TolMatches(:,2))))
  
% FDR search
display('Performing 11Da shift and searching') ;
%[TolFalseMatches] = SearchForMTIDsUsingRange(shiftedMTMassNetDt, calibratedUMCFeatures, massErrorRangeToConsider, netErrorRangeToConsider, dtErrorRangeToConsider) ; 
[TolFalseMatches] = SearchForMTIDs(shiftedMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, 100) ;
sprintf('Identifications with Tolerance refinement and 11 Da shift=  %d', length(unique(TolFalseMatches(:,2)))) 


%search_mass_tol = 10;
%search_net_tol = 0.01;
search_dt_tol = 1;
[TolMatches] = SearchForMTIDs(dbMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
sprintf('Identifications with Tolerance refinement on Mass, NET & DT=  %d',length(unique(TolMatches(:,2))))
 
 
% FDR search
display('Performing 11Da shift and searching') ;
%[TolFalseMatches] = SearchForMTIDsUsingRange(shiftedMTMassNetDt, calibratedUMCFeatures, massErrorRangeToConsider, netErrorRangeToConsider, dtErrorRangeToConsider) ; 
[TolFalseMatches] = SearchForMTIDs(shiftedMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
sprintf('Identifications with Tolerance refinement and 11 Da shift=  %d', length(unique(TolFalseMatches(:,2)))) 

