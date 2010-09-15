function z=z_score(x);
% USAGE function z=zscore(x);
% gives back the z-normalization for x
% if X is a matrix Z is normalized by column
% Z-scores are computed with 
% sample standard deviation (i.e. N-1)
% see zscorepop
[ni,nj]=size(x);
m=mean(x);
s=std(x);
un=ones(ni,1);
z=(x-(un*m))./(un*s); 