\ gpio-simple.fs

: setbit 1 swap lshift ;  \ Calculates the value for bit 0 (LSB) to bit 31 (MSB)

$40014000 constant IO_BANK0_BASE
$4001c000 constant PADS_BANK0_BASE
$d0000000 constant SIO_BASE

SIO_BASE $004 + constant GPIO_IN       \ Input value for GPIO pins
SIO_BASE $010 + constant GPIO_OUT      \ GPIO output value
SIO_BASE $014 + constant GPIO_OUT_SET  \ GPIO output value set
SIO_BASE $018 + constant GPIO_OUT_CLR  \ GPIO output value clear
SIO_BASE $01c + constant GPIO_OUT_XOR  \ GPIO output value XOR
SIO_BASE $020 + constant GPIO_OE       \ GPIO output enable
SIO_BASE $024 + constant GPIO_OE_SET   \ GPIO output enable set
SIO_BASE $028 + constant GPIO_OE_CLR   \ GPIO output enable clear
SIO_BASE $02c + constant GPIO_OE_XOR   \ GPIO output enable XOR

0  setbit constant pin0
1  setbit constant pin1
2  setbit constant pin2
3  setbit constant pin3
4  setbit constant pin4
5  setbit constant pin5
6  setbit constant pin6
7  setbit constant pin7
8  setbit constant pin8
9  setbit constant pin9
10 setbit constant pin10
11 setbit constant pin11
12 setbit constant pin12
13 setbit constant pin13
14 setbit constant pin14
15 setbit constant pin15
16 setbit constant pin16
17 setbit constant pin17
18 setbit constant pin18
19 setbit constant pin19
20 setbit constant pin20
21 setbit constant pin21
22 setbit constant pin22
23 setbit constant pin23
24 setbit constant pin24
25 setbit constant pin25
26 setbit constant pin26
27 setbit constant pin27
28 setbit constant pin28
29 setbit constant pin29

\ Pad control values
%00000001 constant PIN_SlewFast
%00000010 constant PIN_Schmitt
%00000100 constant PIN_PullDown
%00001000 constant PIN_PullUp
%01000000 constant PIN_IE
%10000000 constant PIN_OD
\ Special - Bit 8 set if strength has to be modified, highest given one is set
\ Trap: 4mA 8mA or = 12mA cause of bit logic
%100000000 constant PIN_2mA
%100010000 constant PIN_4mA
%100100000 constant PIN_8mA
%100110000 constant PIN_12mA

: pin2no ( pin -- pinNo ) 	\ results the position of the first true bit from LSB to MSB
  clz 31 swap -				\ clz counts leading zeros, subtract it from 31
;

\ Pad-Bank control
: padRegister ( pin -- Address )         \ Calculates the PadsRegister address
  pin2no 2 lshift 4 +                    \ Starts at BASE + 4 with pin0, Base + 8 pin1 aso
  PADS_BANK0_BASE +
;

\ set pin to output
: pin-output ( pin -- ) GPIO_OE_SET ! ;

\ set output pin to opendrain
: pin-opendrain ( pin -- ) padRegister PIN_OD swap bis! ;

\ set pin to input
: pin-input ( pin -- ) GPIO_OE_CLR ! ;

\ set input pin to pull up, disable pull down
: pin-pu ( pin -- ) padRegister dup PIN_PullUp swap bis! PIN_PullDown swap bic! ;

\ set input pin to pull down, disable pullup
: pin-pd ( pin -- ) padRegister dup PIN_PullDown swap bis! PIN_PullUp swap bic! ;

\ set to botjh pullup and pulldown which is a special case, it remembers last state
: pin-pupd ( pin -- ) padRegister PIN_PullUp PIN_PullDown or swap bis! ;

: pin-high   ( pin -- ) GPIO_OUT_SET ! ;
: pin-low    ( pin -- ) GPIO_OUT_CLR ! ;
: pin-toggle ( pin -- ) GPIO_OUT_XOR ! ;

\ set given pin to value
: pin-set ( value pin -- ) swap	if pin-high else pin-low then ;

\ is given pin set
: pin-set? ( pin -- t/f ) GPIO_IN bit@ ;
