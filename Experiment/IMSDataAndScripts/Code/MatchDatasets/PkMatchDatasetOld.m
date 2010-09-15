% Function to match a LC-IMS-MS dataset to a target DB
%
% Two files are the main input
%               - dLCMS : matirx fo UMC feautures with LC-MS information
%               - dDrifts : matrix of UMC features with drifts information
%
% Output is a an excel file containing output results and a bunch of images
% depicting mass error, alignment and such (similar to VIPER)
%**************************************************************************
%**************************************************************************

function PkMatchDatasetOld(dLCMS,dDrifts);

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

%UMC cols
mz_col = 1 ;
mass_col = 2 ;
scan_col = 3 ;
intensity_col = 4 ;
pair_col = 5 ;
tag_id_col = 6 ;
umc_col = 7 ;
cs_col = 8;

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

% Align with msFeatures
display('Performing LC-MS Warp') ;
[MatchScores,AlignmentFunc,Matches,PepTransformedRT,TransformedRT,MassErrorHist,NetErrorHist] = h.MS2MSMSDBAlignPeptides(dLCMS, dbMassTagInfo) ;
% cols for Matches
% 1:umc_index 2:mtid 3:net_error 4:mass_error
mswarp_identified_mtids = Matches(:, 2) ;
num_umcs_identified = size(mswarp_identified_mtids, 1) ;

% Regress
display('Performing Drift Time Regression') ;
confirmed_drifts = [] ;
observed_drifts = [];
drift_umc_col = 1 ;
drift_col = 3;  % ignoring conformer info in col 2 for the time being
drift_cs_col = 4;

for i = 1:num_umcs_identified
    Iindex = find(dbMassTagsMain(:, dbIdCol) == mswarp_identified_mtids(i)) ;
    confirmed_drifts = [confirmed_drifts; [dbMassTagsMain(Iindex(1), dbIdCol) dbMassTagsMain(Iindex(1), dbDtCol) dbMassTagsMain(Iindex(1), dbCsCol)]] ;
end
observed_drifts = [dDrifts(Matches(:,1), drift_umc_col) dDrifts(Matches(:,1), drift_col) dDrifts(Matches(:,1), drift_cs_col)] ;
[b, bint] = regress(confirmed_drifts(:,2), observed_drifts(:,2)) ; 

save('MaxPeakRegressionData.mat', 'observed_drifts', 'confirmed_drifts');

figure ;
scatter(observed_drifts(:,2), confirmed_drifts(:,2)) ;
hold on ;
Xev = linspace(min(observed_drifts(:,2)), max(observed_drifts(:,2)));
if (length(b) == 1)
    Yev = b* Xev ;
    calibrated_observed_drifts = b* observed_drifts(:,2); 
else
    Yev = b(1)*Xev + b(2) ; 
    calibrated_observed_drifts = b(1) * observed_drifts(:,2) + b(2) ; 
end

plot(Xev, Yev, 'r') ;
xlabel('Observed UMC Drift Time') ; ylabel('Confirmed Mass Tag Drift Time') ;
title('Drift Time Regression') ;

% Calibrate
display('Performing mass, net and drift_time calibration') ;
calibratedUMCFeatures = [] ;
max_mass_shift_ppm = 10000;
for featureNum = 1:numFeatures
    massShiftPPM = -1 *h.GetPPMShift(dLCMS(featureNum, mz_col), dLCMS(featureNum, scan_col));
    if (massShiftPPM < max_mass_shift_ppm)
        massShiftDa = massShiftPPM/1000000 * dLCMS(featureNum, mass_col) ; 
        calibratedMass = dLCMS(featureNum, mass_col) + massShiftDa ;
        indexFeature = find(PepTransformedRT(:,1) == featureNum) ; 
        calibratedNET  = PepTransformedRT(indexFeature, 2) ;
        umcIntensity = dLCMS(featureNum, intensity_col) ; 
%         if (length(b) == 1)
%             calibratedDT = b*dDrifts(featureNum, 3);
%         else
%             calibratedDT = b(1) * dDrifts(featureNum, 3) + b(2) ;
%         end
        calibratedUMCFeatures = [calibratedUMCFeatures ; featureNum calibratedMass calibratedNET dDrifts(featureNum, 3) umcIntensity] ;
    end
end

% Search
display('Searching DB using mass/net tolerances with all wide') ;
search_mass_tol  = 20 ;
search_net_tol = 0.025 ;
search_dt_tol = 100 ;
dbMTMassNetDt = [dbMassTagInfo(:, dbIdCol) dbMassTagInfo(:, dbMassCol) dbMassTagInfo(:, dbNetCol) dbMassTagsMain(:, dbDtCol)] ;
[TrueMatches] = SearchForMTIDs(dbMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
num_identified_mtids = size(unique(TrueMatches(:,2)), 1) ;
sprintf('Number of unique identifications, no refinement =  %d', num_identified_mtids)

% FDR search
display('Performing 11Da shift and searching') ;
shiftedMTMassNetDt = [dbMassTagInfo(:, dbIdCol) dbMassTagInfo(:, dbMassCol)+11 dbMassTagInfo(:, dbNetCol) dbMassTagsMain(:, dbDtCol)] ;
%[FalseMatches] = SearchForMTIDs(shiftedMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
%num_false = size(GetUniqueElements(FalseMatches(:,2)), 1) ;
%sprintf('Number of unique false identifications, no refinement  =  %d',num_false)

% Re-searching using drift time
% dt_error = confirmed_drifts(:,2) - calibrated_observed_drifts ; 
% [mu_dt_tol sigma_dt_tol] = EM_1D(dt_error', 0) ; 
% search_dt_tol = max(abs([mu_dt_tol+sigma_dt_tol mu_dt_tol-sigma_dt_tol])) ; 

display('Performing tolerance refinement using EM algorithm')
% mass_err = TrueMatches(:, 3)';
% net_err = TrueMatches(:, 4)' ; 
% dt_err = TrueMatches(:, 5)' ; 
% [sigma_mass mu_mass] = EM_1D(mass_err,0) ; 
% [sigma_net mu_net] = EM_1D(net_err,0) ; 
% [sigma_dt mu_dt] = EM_1D(dt_err,0) ; 

%search_mass_tol = max(abs([mu_mass-2*sigma_mass mu_mass+2*sigma_mass])) ; 
%search_net_tol = max(abs([mu_net-2*sigma_net mu_net+2*sigma_net])) ; 

search_mass_tol = 10;
search_net_tol = 0.01;
search_dt_tol = 1;
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
sprintf('********************************************************************************\n');
sprintf('Tolerances for searching %3.4f, %3.4f \n', search_mass_tol, search_net_tol)
[TolMatches] = SearchForMTIDs(dbMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, 100) ;
sprintf('Number of unique identifications after tolerance refinement =  %d',length(GetUniqueElements(TolMatches(:,2))))
 
% FDR search
display('Performing 11Da shift and searching') ;
[TolFalseMatches] = SearchForMTIDs(shiftedMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, 100) ;
sprintf('Number of unique false identifications after tolerance refinement =  %d', length(GetUniqueElements(TolFalseMatches(:,2)))) 
sprintf('********************************************************************************\n');
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
sprintf('********************************************************************************\n');
sprintf('Tolerances for searching %3.4f, %3.4f %3.4f\n', search_mass_tol, search_net_tol, search_dt_tol) ;
[TolMatches] = SearchForMTIDs(dbMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
sprintf('Number of unique identifications after tolerance refinement =  %d',length(GetUniqueElements(TolMatches(:,2))))
display('Performing 11Da shift and searching') ;
[TolFalseMatches] = SearchForMTIDs(shiftedMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
sprintf('Number of unique false identifications after tolerance refinement =  %d', length(GetUniqueElements(TolFalseMatches(:,2)))) 
sprintf('********************************************************************************\n');
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%