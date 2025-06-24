\ ST7565 based LCD driver

#require gpio-simple.fs
#require spi.fs

#require st7565-font.fs

\ 10     | SPI1 SCK
\ 11     | SPI1 MOSI
\ 12     | SPI1 MISO

\ define pins used for CS and A0
pin13 constant LCD_CS
pin9 constant LCD_A0

\ viki2 red led
pin14 constant LCD_REDLED

128 constant LCD_WIDTH
64 constant LCD_HEIGHT
LCD_HEIGHT 7 + 8 / constant LCD_PAGES

: lcd-commands ( n buf -- )
    LCD_CS pin-low
    LCD_A0 pin-low
    spi1-write
    LCD_CS pin-high
;

: lcd-data ( n buf -- )
    LCD_CS pin-low
    LCD_A0 pin-high
    spi1-write
    LCD_CS pin-high
    LCD_A0 pin-low
;

16 buffer: lcd_buf
: lcd-buf! ( c # -- ) lcd_buf + c! ;
: lcd-buf@ ( # -- u ) lcd_buf + c@ ;

: lcd-init
	LCD_CS pin-output
	LCD_CS pin-high
	LCD_A0 pin-output
	LCD_A0 pin-high

	LCD_REDLED pin-output
	LCD_REDLED pin-low

    $40 0 lcd-buf!	    \ Display start line 0
    $A0 1 lcd-buf!    	\ ADC
    $c8 2 lcd-buf! 		\ COM select
    $a6 3 lcd-buf!    	\ Display normal
    $a2 4 lcd-buf!    	\ Set Bias 1/9 (Duty 1/65)
    $2f 5 lcd-buf!    	\ Booster Regulator and Follower On
    $f8 6 lcd-buf!    	\ Set internal Booster to 4x
    $00 7 lcd-buf!
    $27 8 lcd-buf! 		\ Contrast set
    $81 9 lcd-buf!
    $09 10 lcd-buf!    	\ contrast value
    $ac 11 lcd-buf!    	\ No indicator
    $00 12 lcd-buf!
    $af 13 lcd-buf!    	\ Display on
    14 lcd_buf lcd-commands
;

\ clamp x between low and high
: lcd-clamp ( x low high -- nx )
	dup 3 pick < if -rot 2drop exit then
	drop
	dup 2 pick > if nip exit then
	drop
;

\ NOTE x is pixel x but y is the page# (which is 8 vertical pixels)
: lcd-xy ( x y -- )
	\ clamp x and y
	0 LCD_PAGES 1- lcd-clamp swap
	0 LCD_WIDTH 1- lcd-clamp swap

    $07 and $B0 or 0 lcd-buf!
    dup 4 rshift $10 or 1 lcd-buf!
    $0F and $00 or 2 lcd-buf!
    3 lcd_buf lcd-commands
;

\ we space by 6 even though the font is 5 pixels, so we get nice spacing
\ however this leaves a gap if using line art
: lcd-rowcol ( row col -- ) 6 * swap lcd-xy ;

: lcd-contrast ( n -- )
    $27 0 lcd-buf!	    \ Contrast set
    $81 1 lcd-buf!
        2 lcd-buf!		\ contrast value
 	3 lcd_buf lcd-commands
;

LCD_WIDTH buffer: lcd_linebuf \ 128x8 linebuffer
: lcd-clear ( color -- )
	lcd_linebuf LCD_WIDTH rot fill
	\ clear in 128x8 blocks
    LCD_PAGES 0 do  \ each page is 8 vertical columns of pixels
		0 i lcd-xy
    	LCD_WIDTH lcd_linebuf lcd-data
    loop
;

\ clears a box starting at row/col for #rows and #cols
: lcd-clear-box ( #rows #cols row col -- )
	lcd_linebuf LCD_WIDTH 0 fill
	lcd-rowcol
    swap 0 do
    	6 * lcd_linebuf lcd-data
    loop
;

: lcd-clear-row ( row -- ) >r 1 LCD_WIDTH 6 / r> 0 lcd-clear-box ;

\ puts a character into the LCD at the current position
: lcd-char ( c -- )
    5 *  							\ char index
    5 0 do  						\ 5 columns of pixels
    	dup i + 5x7FONT + c@  		\ fetch column from font
    	lcd_linebuf i + c!			\ write into linebuf
    loop
    drop
    5 lcd_linebuf lcd-data
;

\ initially y is the line/page
: lcd-draw-char ( c x y -- )
    \ TODO make sure that y is on a page boundary and x + 6 is < LCD_WIDTH
    lcd-xy
    lcd-char
;

\ returns the next col and same row (until we allow wrap)
: lcd-write-str ( row col str-addr len -- nrow ncol )
	0 do 						\ -- row col str-addr
		dup i + c@				\ -- row col str-addr char
		3 pick 3 pick lcd-rowcol
		lcd-char 				\ -- row col str-addr
		swap 1+ swap 			\ next col
	loop
	drop
;

: lcd-write-cstr ( row col cstr-addr -- nrow ncol ) count lcd-write-str ;

: n>str s>d TUCK DABS <#  #S ROT SIGN  #> ;
: lcd-printn ( row col n -- nrow ncol ) n>str lcd-write-str ;

: test-lcd
	10 11 12 spi1-set-pins if ." invalid pins for SPI1" exit then
	1000000 spi1-init
	lcd-init
	18 lcd-contrast  \ mini viki
	begin
		LCD_REDLED pin-high
		0 lcd-clear
		1000 ms
		$FF lcd-clear
		LCD_REDLED pin-low
		1000 ms
		$0F lcd-clear
		LCD_REDLED pin-high
		1000 ms
		$F0 lcd-clear
		LCD_REDLED pin-low
		1000 ms
	key? until
;

: test-chars
	10 11 12 spi1-set-pins if ." invalid pins for SPI1" exit then
	1000000 spi1-init
	lcd-init
	18 lcd-contrast  \ mini viki

	2 0 do
		0 lcd-clear
		i 128 *
		LCD_PAGES 0 do 				\ each line
			16 0 do  				\ each character on line
				dup i + 			\ character
				i 6 * 12 +			\ x pos
				j					\ y pos
				lcd-draw-char
			loop
			16 +
		loop
		drop
		key drop
	loop
;

0 variable tcnt
: splash1 c" *********************" ;
: splash2 c" *        Test       *" ;

create splash12
	$c9 c, $cd c, $cd c, $cd c, $cd c, $cd c, $cd c, $cd c,
	$cd c, $cd c, $cd c, $cd c, $cd c, $cd c, $cd c, $cd c,
	$cd c, $cd c, $cd c, $cd c, $bb c,
21 constant splash12_len

create splash22
	$ba c,

create splash32
	$c8 c, $cd c, $cd c, $cd c, $cd c, $cd c, $cd c, $cd c,
	$cd c, $cd c, $cd c, $cd c, $cd c, $cd c, $cd c, $cd c,
	$cd c, $cd c, $cd c, $cd c, $bc c,
21 constant splash32_len


: test2
  0 lcd-clear
  \ 0 0 splash1 lcd-write-cstr 2drop
  \ 1 0 splash2 lcd-write-cstr 2drop
  \ 2 0 splash1 lcd-write-cstr 2drop

  \ NOTE these are actually 5 pixels long so to do line art we need to space 5 not 6
  0 0 splash12 splash12_len lcd-write-str 2drop
  1 0 splash22 1 lcd-write-str s"        Test        " lcd-write-str splash22 1 lcd-write-str 2drop
  2 0 splash32 splash32_len lcd-write-str 2drop

  3 0 c" count: " lcd-write-cstr \ -- nrow ncol
  begin
    1 tcnt +!
    1 4 3 7 lcd-clear-box		\ just clear the count
    tcnt @ lcd-printn drop 7  	\ move column back to 7
    100 ms
    tcnt @ 200 > if 0 tcnt ! then
  key? until
;
