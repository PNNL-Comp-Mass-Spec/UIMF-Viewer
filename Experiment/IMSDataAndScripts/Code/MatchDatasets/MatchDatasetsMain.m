%Main Program to match the results of deisotoping to a target database
%using NET, mass and Drift Time.  Input is basically a file path that
%contains all the individual datasets to match, along with the target DB.
%The results of peptide matching are stored in each individual folder and
%the number of Mass Tags identified per dataset are reported in the command line
read = 1;
if read == 1
    clear all ; 
    % Get main directory
    directoryname = uigetdir('C:\DriftTimePrediction\QC_Shew\Test', 'Pick Directory To process');
    if directoryname == 0
        return ;
    end

    [databaseFile, path_name] = uigetfile('C:\DriftTimePrediction\QC_Shew\*.xls', 'Pick database') ;
    if (databaseFile == 0)
        return ;
    end
    fileName = strcat(path_name, databaseFile) ; 
    [dataBase, text] = xlsread(fileName) ; 
end
starttime = clock;
global DBMassTags ; 
DBMassTags = []; 

STDVOL = 1800;
% Initialization
%% column numbers in input database
mass_tag_id_column = 1 ;

% 
% mass_tag_peptide_column = 2 ;
% mass_tag_mass_column = 3 ;
% mass_tag_net_column = 4 ;
% mass_tag_cs_column = 5;
% mass_tag_mobility_column = 6 ;
% mass_tag_crossection_column = 7 ;
% mass_tag_slope_column = 8;
% mass_tag_intercept_column = 9;
% mass_tag_standard_dt_column = 11 ; 

conformer_idx_column = 2;
mass_tag_peptide_column = 3 ;
mass_tag_mass_column = 4 ;
mass_tag_net_column = 5 ;
mass_tag_cs_column = 6;
mass_tag_mobility_column = 7 ;
mass_tag_crossection_column = 8 ;
mass_tag_slope_column = 9;
mass_tag_intercept_column = 10;
mass_tag_standard_dt_column = 12 ; 



completed_process = 0 ;

standard_pressure = 4 ;

%% If the DB does not have slope and intercept for each MTID, then we need
%% to calculate using a standard intercept that's guessestimated from
%% analysis. 
%% standard_intercept = 0.88 ;

V = (STDVOL-118) ;
avg_temp = 294.1 ;
drift_tube_length = 98 ;
buffer_gas = 28.01348 ;

% Get mass and drift times for all MTIDS in DBmatrix
mass_tag_masses = dataBase(:, mass_tag_mass_column);
mass_tag_omegas = dataBase(:, mass_tag_crossection_column) ;
mass_tag_cs = dataBase(:, mass_tag_cs_column) ;
mass_tag_ids = dataBase(:, mass_tag_id_column) ; 
mass_tag_peptides = text(2:size(text,1), mass_tag_peptide_column) ; 
mass_tag_nets = dataBase(:, mass_tag_net_column) ; 
mass_tag_drifts = dataBase(:, mass_tag_standard_dt_column) ;  %% Since I'm using a 1.8KV dataset no need of conversion
conf_indices = dataBase(:, conformer_idx_column);
mass_tag_slopes = dataBase(:, mass_tag_slope_column) ; 
mass_tag_intercepts = dataBase(:, mass_tag_intercept_column) ; 

num_mass_tags = size(mass_tag_masses, 1) ;

% DbColumns for MSWarp
% num_cols = 7 ; 
% net_col = 1 ; 
% mass_col = 2 ; 
% num_obs_col = 3 ; 
% id_col = 4 ; 
% xcorr_col = 5 ; 
% dt_col = 6 ;
% cs_col = 7;


num_cols = 8 ; 
net_col = 1 ; 
mass_col = 2 ; 
num_obs_col = 3 ; 
id_col = 4 ; 
xcorr_col = 5 ; 
dt_col = 6 ;
cs_col = 7;
confId_col = 8;

DBMassTags = zeros(num_mass_tags, num_cols) ; 
xcorrs = zeros(num_mass_tags,1) + 7 ; 
numobs = zeros(num_mass_tags,1) + 10 ; 
DBMassTags(:, [net_col mass_col id_col dt_col]) = [mass_tag_nets mass_tag_masses mass_tag_ids mass_tag_drifts] ; 
DBMassTags(:, xcorr_col) = xcorrs ; 
DBMassTags(:, num_obs_col) = numobs ; 
DBMassTags(:, cs_col) = mass_tag_cs ; 
DBMassTags(:, confId_col) = conf_indices ; 
DBMassTags = {DBMassTags mass_tag_peptides}; %mass_tag_protein} ; 

% start readin in each drifts file
current_dir = pwd ;
cd(directoryname) ;
files = dir ('*_drifts.csv')  ;
num_files = size(files,1) ;
save ('MaxPeakDB.mat', 'DBMassTags');


if (num_files == 0)
    % nothin here, so time to look in the subfolders
    list = dir(directoryname) ;
    num_directories = size(list,1) ;
    for dir_num = 3: num_directories
        dir_name = list(dir_num,:).name ;
        cd (dir_name) ;
        files = dir('*_drifts.csv') ;
        num_files = size(files, 1) ;
        for file_num = 1:num_files
            file_name = files(file_num).name ;

            % set path name
            pathname = strcat(directoryname, '\') ;
            pathname = strcat(pathname, dir_name) ;
            pathname = strcat(pathname, '\') ;
            pathname = strcat(pathname, file_name) ;

            %process
            sprintf ('Processing file %s', file_name) 
            cd(current_dir) ;
            ProcessDataset(pathname) ;
            cd(directoryname)  ;
        end
    end
else
    % process all drifts file in this dataset
    for file_num = 1:num_files
        file_name = files(file_num).name ;
        pathname = strcat(directoryname, '\') ;
        pathname = strcat(pathname, file_name) ;

        %process
        sprintf ('Processing file %s', file_name)
        cd(current_dir) ;
        ProcessDataset(pathname) ; 
        cd(directoryname) ;
    end
    completed_process = 1 ;
    
end

cd(current_dir) ;
endtime = clock;
sprintf('Time taken to run script %f seconds.\n' , etime(endtime, starttime))

display('Done') ;