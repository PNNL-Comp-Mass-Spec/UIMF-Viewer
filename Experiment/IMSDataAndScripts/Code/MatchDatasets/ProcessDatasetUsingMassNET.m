function ProcessDatasetUsingMassNet

clear all ;

%     [databaseFile, path_name] = uigetfile('C:\data\IMS\DB_Creation\.xls', 'Pick database') ;
%     if (databaseFile == 0)
%         return ;
%     end
%     fileName = strcat(path_name, databaseFile) ;
fileName = 'C:\data\IMS\DB_Creation\ShewUsing10hrADC\Round3_Method2_MoreConfident\Meth2_ShewP325_ADC_AMT_MoreConfident_with_crossection.xls' ;
[dataBase, text] = xlsread(fileName) ;

dataset = 'C:\data\IMS\IMSPkMatching\OrbData\QC_Shew_08_01_pt1-b_16Jun08_Sphinx_08-04-16_UMC_List.csv' ;
featuresData = csvread(dataset, 1, 0) ;


DBMassTags = [];

% Initialization
%% column numbers in input database
mass_tag_id_column = 1 ;
mass_tag_peptide_column = 2 ;
%mass_tag_protein_column = 3 ;
mass_tag_mass_column = 3 ;
mass_tag_net_column = 4 ;
mass_tag_cs_column = 5 ;
mass_tag_mobility_column = 6 ;
mass_tag_crossection_column = 7 ;
mass_tag_standard_dt_column = 11 ;


% Get mass and drift times for all MTIDS in DBmatrix
mass_tag_masses = dataBase(:, mass_tag_mass_column);
mass_tag_omegas = dataBase(:, mass_tag_crossection_column) ;
mass_tag_cs = dataBase(:, mass_tag_cs_column) ;
mass_tag_ids = dataBase(:, mass_tag_id_column) ;
mass_tag_peptides = text(2:size(text,1), mass_tag_peptide_column) ;
%mass_tag_protein = text(2:size(text,1), mass_tag_protein_column);
mass_tag_nets = dataBase(:, mass_tag_net_column) ;
mass_tag_drifts = dataBase(:, mass_tag_standard_dt_column) ;  %% Since I'm using a 1.8KV dataset no need of conversion


% DbColumns for MSWarp

num_mass_tags = size(mass_tag_masses, 1) ;
num_cols = 6 ;
net_col = 1 ;
mass_col = 2 ;
num_obs_col = 3 ;
id_col = 4 ;
xcorr_col = 5 ;
dt_col = 6 ;

DBMassTags = zeros(num_mass_tags, num_cols) ;
xcorrs = zeros(num_mass_tags,1) + 7 ;
numobs = zeros(num_mass_tags,1) + 10 ;
DBMassTags(:, [net_col mass_col id_col dt_col]) = [mass_tag_nets mass_tag_masses mass_tag_ids mass_tag_drifts] ;
DBMassTags(:, xcorr_col) = xcorrs ;
DBMassTags(:, num_obs_col) = numobs ;
DBMassTags = {DBMassTags mass_tag_peptides}; %mass_tag_protein} ;



features_index_col = 1;
features_rep_scan_col = 10 ;
features_rep_mass_col = 12;
features_mz_col = 13 ;
features_intensity_col = 14 ;
numFeatures = length(featuresData) ;
dLCMS = zeros(numFeatures, 1) ;
dLCMS = zeros(numFeatures, 7) ;
mz_col = 1 ;
mass_col = 2 ;
scan_col = 3 ;
intensity_col = 4 ;
pair_col = 5 ;
tag_id_col = 6 ;
umc_col = 7 ;
dLCMS(:, [mz_col mass_col scan_col]) = [featuresData(:, features_mz_col) featuresData(:, features_rep_mass_col) featuresData(:, features_rep_scan_col)] ;
dLCMS(:, [intensity_col])= featuresData(:, features_intensity_col) ;
dLCMS(:, [umc_col])= [1:numFeatures];

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

%UMC cols
mz_col = 1 ;
mass_col = 2 ;
scan_col = 3 ;
intensity_col = 4 ;
pair_col = 5 ;
tag_id_col = 6 ;
umc_col = 7 ;

% For LCMSWarp

h = actxserver('MassMatchCOM.MassMatchWrapper') ;

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



% Calibrate
display('Performing mass and net calibration') ;
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
        calibratedDT = 0 ;
        calibratedUMCFeatures = [calibratedUMCFeatures ; featureNum calibratedMass calibratedNET calibratedDT umcIntensity] ;
    end
end

% Search
display('Searching DB using mass/net tolerances') ;
search_mass_tol  = 20 ;
search_net_tol = 0.25 ;
search_dt_tol = 100 ;
dbMTMassNetDt = [dbMassTagInfo(:, dbIdCol) dbMassTagInfo(:, dbMassCol) dbMassTagInfo(:, dbNetCol) dbMassTagsMain(:, dbDtCol)] ;
[TrueMatches] = SearchForMTIDs(dbMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
num_identified_mtids = size(GetUniqueElements(TrueMatches(:,2)), 1) ;
sprintf('Number of unique identifications =   %d', num_identified_mtids)



% FDR search
display('Performing 11Da shift and searching') ;
shiftedMTMassNetDt = [dbMassTagInfo(:, dbIdCol) dbMassTagInfo(:, dbMassCol)+11 dbMassTagInfo(:, dbNetCol) dbMassTagsMain(:, dbDtCol)] ;
[FalseMatches] = SearchForMTIDs(shiftedMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
num_false = size(GetUniqueElements(FalseMatches(:,2)), 1) ;
sprintf('Number of unique false identifications =  %d',num_false)


display('Performing tolerance refinement using EM algorithm')
mass_err = TrueMatches(:, 3)';
net_err = TrueMatches(:, 4)' ;
[sigma_mass mu_mass] = EM_1D(mass_err,0) ;
[sigma_net mu_net] = EM_1D(net_err,0) ;
search_mass_tol = abs(max([mu_mass-2*sigma_mass mu_mass+2*sigma_mass])) ;
search_net_tol = abs(max([mu_net-2*sigma_net mu_net+2*sigma_net])) ;
[TolMatches] = SearchForMTIDs(dbMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
num_identified_mtids = size(GetUniqueElements(TolMatches(:,2)), 1) ;
sprintf('Number of unique identifications after tol. refinement =  %d', num_identified_mtids)

display('Performing 11Da shift and searching') ;
shiftedMTMassNetDt = [dbMassTagInfo(:, dbIdCol) dbMassTagInfo(:, dbMassCol)+11 dbMassTagInfo(:, dbNetCol) dbMassTagsMain(:, dbDtCol)] ;
[TolFalseMatches] = SearchForMTIDs(shiftedMTMassNetDt, calibratedUMCFeatures, search_mass_tol, search_net_tol, search_dt_tol) ;
num_false = size(GetUniqueElements(TolFalseMatches(:,2)), 1) ;
sprintf('Number of unique false identifications after tol. refinement =  %d',num_false)

end



