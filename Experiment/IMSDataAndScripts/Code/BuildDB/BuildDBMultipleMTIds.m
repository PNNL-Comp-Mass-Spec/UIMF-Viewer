% Program to build an AMT-like database that contains features that include 
% NET, mass , cross-section and slope and intercept of DT vs P/V fit
% 
%
% Input files are outputs from GetDriftTime.m program that
% correspond to 4 P/V points, so run that program prior to running this
% 
%
% Output is in the form of xls document

%**************************************************************************
%*************************************************************************

clear all ;
clc ;

% column headers for input
amt_mass_tag_id_column = 1 ;
amt_peptide_column = 2 ;
%amt_protein_column = 3 ;
amt_mass_tag_mass_column = 3 ;
amt_mass_tag_net_column = 4 ;
drift_time_column = 5;
charge_state_column = 6 ;
pv_column = 7 ;

% data_structures
mass_tag_ids = [] ;
mass_tag_masses= [] ;
mass_tag_nets = [] ;
mass_tag_peptide = [] ;
mass_tag_protein = [] ;

drifts_vector = [] ;
pv_vector = [] ;
cs_vector = [] ; 

% %Enter input 
%% uncomment for user input
% avg_temp = input('Enter average temperature in K:   ') ;
% drift_tube_length = input('Enter drift tube length: ') ;
% buffer_gas = input('Enter buffer gas mass:  ') ;

num_files = input('Enter number of P/V points:  ') ;
num_consider = input('Enter minimum number of P/V points to consider:   ') ; 
min_R2_value = input ('Enter minimum R2_value to consider:   ') ; 

avg_temp = 294.1 ; 
drift_tube_length = 98 ; 
buffer_gas = 28.01348 ; 
current_dir = pwd ;  

for file_num = 1:num_files
    % Choose file
    [file_name, path_name]   = uigetfile('*.xls', 'Choose xls file') ;
    if file_name == 0
        return ;
    end
      
    cd(path_name) ;
    [mass_tag_data, txt] = xlsread(file_name) ;
    

    % get information
    mass_tag_ids =  [mass_tag_ids ; mass_tag_data(:, amt_mass_tag_id_column)] ;
    mass_tag_nets = [mass_tag_nets ; mass_tag_data(:, amt_mass_tag_net_column)] ;
    mass_tag_masses = [mass_tag_masses ; mass_tag_data(:, amt_mass_tag_mass_column)];
    mass_tag_peptide = [mass_tag_peptide ; txt(2:size(txt,1), amt_peptide_column)] ;
    %mass_tag_protein =  [mass_tag_protein ; txt(2:size(txt,1), amt_protein_column)] ;
    drifts_vector = [drifts_vector; mass_tag_data(:, drift_time_column)] ;
    pv_vector = [pv_vector ; mass_tag_data(:, pv_column)] ;
    cs_vector = [cs_vector ; mass_tag_data(:, charge_state_column)] ;
end

cd(current_dir) ; 
num_entries = size(mass_tag_ids, 1) ;


%output_header
output_cells = {'Mass_Tag_ID', 'Peptide', 'Mass_Tag_Mass', 'Mass_Tag_NET', 'Charge State', 'Mobility', 'Cross-section', 'Slope', 'Intercept', 'R2_Value', 'DT_Standard'} ;


% now all P/V points are entered, start to fit a line for each mass_tag_id
% guy
i = 1;
while i < num_entries
               
        id = mass_tag_ids(i) ;
               
        I = find(mass_tag_ids == id) ;                

        %clear from mass_tag_ids so we don't reprocepess
        mass_tag_ids(I) = 0 ;
        
        %see if we have enough points
        if (size(I, 1) >= num_consider)
            drift_points = drifts_vector(I) ;
            pv_points = pv_vector(I) ;
            cs_points = cs_vector(I) ; 

            mass = mass_tag_masses(I(1)) ;
            net = mass_tag_nets(I(1)) ;
            peptide = mass_tag_peptide(I(1)) ;
            %protein = mass_tag_protein(I(1)) ;
            
            %get most observed charge state
            [p_cs, bin_cs] = hist(cs_points, [1 2 3 4 5 6 7 8 9 10]) ; 
            [y_cs, i_cs] = max(p_cs) ;
            cs = bin_cs(i_cs) ; 
            
            % fit lines
            n = length(drift_points) ; 
            x1 = [ones(n,1), pv_points] ;              
            [beta, bint, r, rint, stats] = regress(drift_points, x1) ; 
            slope = beta(2) ; 
            intercept = beta(1) ; 
            r2 = stats(1) ; 
            if (r2 > min_R2_value)
                %calculate
                [k0 omega] = calculate_cross_section(cs, slope, avg_temp, mass, drift_tube_length, buffer_gas) ;
                dt_standard = BackCalculateDriftTime(omega, avg_temp, intercept, cs, mass, buffer_gas, (1800-118), drift_tube_length, slope) ; 
                row = {id peptide{1} mass net cs k0 omega slope intercept r2 dt_standard } ;
                output_cells = [output_cells; row] ;
            end
        end
   
    i = i +1 ;
end

%write out results to a DB-like xls file
cd(path_name) ;
[newmatfile, newpath] = uiputfile('*.xls', 'Save As');
filename = [newpath,newmatfile];
if isempty(findstr([newpath,newmatfile],'.xls'))
    filename = [filename,'.xls'];
end
fid = fopen(filename, 'r') ;
if (fid > -1)
    fclose(fid) ;
    delete(filename) ; % so as to overwrite the damn thing   
end
clear fid ;
s = xlswrite(filename, output_cells) ;
cd (current_dir) ;