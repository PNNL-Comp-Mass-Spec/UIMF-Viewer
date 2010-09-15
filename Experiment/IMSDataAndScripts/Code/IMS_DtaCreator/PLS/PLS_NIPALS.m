function [T, P, C, Bpls]=PLS_NIPALS(X, Y, nfactor)



% Modified from Original Code by Anoop Mayampurath May 2007
% for IMS_DTA_Creator

% 
% PLS regression NIPALS algorithm
% Compute the PLS regression coefficients
% X=T*P' Y=T*B*C'=X*Bpls  X and Y being Z-scores
%                          B=diag(b)
% Y=X*Bpls_star with X being augmented with a col of ones
%                       and Y and X having their original units
% T'*T=I (NB normalization <> SAS)
% %
% Test for PLS regression
% Herve Abdi November 2002/rev November 2004
%
%
% Version with T, W, and C being unit normalized
% U, P are not
% nfact=number of latent variables to keep
% default = rank(X)



if exist('nfactor')~=1;nfactor=rank(X);end
M_X=mean(X);
M_Y=mean(Y);
S_X=std(X);
S_Y=std(Y);

%col = 1 ; 
% 
% % choose a non-zero coloumn choosing on the most abundant peak
 sum_intensities = sum(X)  ; 
 max_intensity  = max(sum_intensities) ; 
 col= find(sum_intensities == max_intensity) ; 



X=zscore(X);
Y=zscore(Y);

bool_save_info = 1 ; 

[nn,np]=size(X) ;
[n,nq]=size(Y)  ;
if nn~= n;
    error(['Incompatible # of rows for X and Y']);
end

% Precision for convergence
epsilon=eps('double');

% Initialistion of the Y set
U=zeros(n,nfactor);
C=zeros(nq,nfactor);

% Initialisation of the X set
T=zeros(n,nfactor);
P=zeros(np,nfactor);
W=zeros(np,nfactor);
b=zeros(1,nfactor);
R2_X=zeros(1,nfactor);
R2_Y=zeros(1,nfactor);

SS_X=sum(sum(X.^2));
SS_Y=sum(sum(Y.^2));

% Start 
for l=1:nfactor       
 t=normaliz(Y(:,col));
 t0=normaliz(rand(n,1)*10);
 u=t;
 nstep=0;
 maxstep=50;
  while ( ( (t0-t)'*(t0-t) > epsilon/2) & (nstep < maxstep)); 
   nstep=nstep+1;
   disp(['Latent Variable #',int2str(l),'  Iteration #:',int2str(nstep)])
   t0=t;
   w=normaliz(X'*u);
   t=normaliz(X*w);
   c=normaliz(Y'*t);
   u=Y*c;
  end;
  disp(['Latent Variable #',int2str(l),', convergence reached at step ',...
    int2str(nstep)]);

 % t has converged
 
 % Get loading vector and adjust X 
 p=X'*t;
 X = X-t*p';
 
 % Regression score and adjust Y
 b_l=((t'*t)^(-1))*(u'*t); 
 Y = Y-(b_l*(t*c')); 
 
 % Store in matrices
 b(l)=b_l;
 P(:,l)=p;
 W(:,l)=w;
 T(:,l)=t;
 U(:,l)=u;
 C(:,l)=c;
end


% Done

% Get Bpls
Wstar=W*inv(P'*W);
Bpls=Wstar*diag(b)*C';

if bool_save_info 
    pack  ; 
    save mall_training_values
end

% Get Yhat
%Yhat=T*diag(b)*C';







