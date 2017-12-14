set terminal wxt size 800,600 enhanced font 'Lato Light,12' persist

# Line width of the axes
set border linewidth 1.5
set key outside

plot for [col=2:8] 'region.dat' using 1:col with lines title columnheader linewidth 2
    
pause -1 'press Ctrl-D to exit