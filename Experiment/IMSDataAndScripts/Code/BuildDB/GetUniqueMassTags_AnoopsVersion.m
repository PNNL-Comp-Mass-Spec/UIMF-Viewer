function [out1] = GetUniqueMassTags(mass_tags) 
    %% simple function to calculate unique mass tags from an array of mass
    %% tags
    sorted_mass_tags = sort(mass_tags) ; 
    I = [2: size(sorted_mass_tags, 1)] ; 
    diff = sorted_mass_tags(I) - sorted_mass_tags(I-1) ; 
    diff = [diff ; 1];
    I2 = find(diff>0) ; 
    out1 = sorted_mass_tags(I2) ; 
    return ; 
end