#require spi.fs
#require gpio-simple.fs
#require cycles.fs

$00 constant  ILI9341_NOP                   \ No-op
$01 constant  ILI9341_SWRESET               \ Software reset
$04 constant  ILI9341_RDDID                 \ Read display ID info
$09 constant  ILI9341_RDDST                 \ Read display status
$10 constant  ILI9341_SLPIN                 \ Enter sleep mode
$11 constant  ILI9341_SLPOUT                \ Exit sleep mode
$12 constant  ILI9341_PTLON                 \ Partial mode on
$13 constant  ILI9341_NORON                 \ Normal display mode on
$0A constant  ILI9341_RDMODE                \ Read display power mode
$0B constant  ILI9341_RDMADCTL              \ Read display MADCTL
$0C constant  ILI9341_RDPIXFMT              \ Read display pixel format
$0D constant  ILI9341_RDIMGFMT              \ Read display image format
$0F constant  ILI9341_RDSELFDIAG            \ Read display self-diagnostic
$20 constant  ILI9341_INVOFF                \ Display inversion off
$21 constant  ILI9341_INVON                 \ Display inversion on
$26 constant  ILI9341_GAMMASET              \ Gamma set
$28 constant  ILI9341_DISPLAY_OFF           \ Display off
$29 constant  ILI9341_DISPLAY_ON            \ Display on
$2A constant  ILI9341_SET_COLUMN            \ Column address set
$2B constant  ILI9341_SET_PAGE              \ Page address set
$2C constant  ILI9341_WRITE_RAM             \ Memory write
$2E constant  ILI9341_READ_RAM              \ Memory read
$30 constant  ILI9341_PTLAR                 \ Partial area
$33 constant  ILI9341_VSCRDEF               \ Vertical scrolling definition
$36 constant  ILI9341_MADCTL                \ Memory access control
$37 constant  ILI9341_VSCRSADD              \ Vertical scrolling start address
$3A constant  ILI9341_PIXFMT                \ COLMOD: Pixel format set
$51 constant  ILI9341_WRITE_DISPLAY_BRIGHTNESS               \ Brightness hardware dependent!
$52 constant  ILI9341_READ_DISPLAY_BRIGHTNESS
$53 constant  ILI9341_WRITE_CTRL_DISPLAY
$54 constant  ILI9341_READ_CTRL_DISPLAY
$55 constant  ILI9341_WRITE_CABC               \ Write Content Adaptive Brightness Control
$56 constant  ILI9341_READ_CABC                \ Read Content Adaptive Brightness Control
$5E constant  ILI9341_WRITE_CABC_MINIMUM       \ Write CABC Minimum Brightness
$5F constant  ILI9341_READ_CABC_MINIMUM        \ Read CABC Minimum Brightness
$B1 constant  ILI9341_FRMCTR1                  \ Frame rate control (In normal mode/full colors)
$B2 constant  ILI9341_FRMCTR2                  \ Frame rate control (In idle mode/8 colors)
$B3 constant  ILI9341_FRMCTR3                  \ Frame rate control (In partial mode/full colors)
$B4 constant  ILI9341_INVCTR                   \ Display inversion control
$B6 constant  ILI9341_DFUNCTR                  \ Display function control
$C0 constant  ILI9341_PWCTR1                   \ Power control 1
$C1 constant  ILI9341_PWCTR2                   \ Power control 2
$CB constant  ILI9341_PWCTRA                   \ Power control A
$CF constant  ILI9341_PWCTRB                   \ Power control B
$C5 constant  ILI9341_VMCTR1                   \ VCOM control 1
$C7 constant  ILI9341_VMCTR2                   \ VCOM control 2
$DA constant  ILI9341_RDID1                    \ Read ID 1
$DB constant  ILI9341_RDID2                    \ Read ID 2
$DC constant  ILI9341_RDID3                    \ Read ID 3
$DD constant  ILI9341_RDID4                    \ Read ID 4
$E0 constant  ILI9341_GMCTRP1                  \ Positive gamma correction
$E1 constant  ILI9341_GMCTRN1                  \ Negative gamma correction
$E8 constant  ILI9341_DTCA                     \ Driver timing control A
$EA constant  ILI9341_DTCB                     \ Driver timing control B
$ED constant  ILI9341_POSC                     \ Power on sequence control
$F2 constant  ILI9341_ENABLE3G                 \ Enable 3 gamma control
$F7 constant  ILI9341_PUMPRC                   \ Pump ratio control

240 constant ILI9341_defWIDTH
320 constant ILI9341_defHEIGHT

ILI9341_defWIDTH  variable ILI9341_WIDTH
ILI9341_defHEIGHT variable ILI9341_HEIGHT
ILI9341_WIDTH @ ILI9341_HEIGHT @ * constant ILI9341_SCREEN_SIZE

\ for h745 which does not have c, this needs to be changed
: ec, c, ;
: ecalign ;

\ #require font20.fs
#require font16.fs
\ #require font8.fs

Font16 variable current_font
: set-font ( font_address -- ) current_font ! ;
: font-width current_font @ c@ ;
: font-height current_font @ 1+ c@ ;
: font-stride font-width 1- 8 / 1+ ;
: font-data ( char -- addr )        \ returns the address of the start of the font data
    $20 - dup 0< if drop $20 then   \ starts at space $20
    font-stride *
    font-height *
    current_font @ 2+ +
;
: font-row ( fontaddr row -- 32bit left aligned pixels )
    font-stride * +         \ address of this row of font pixels
                            \ fetch 3 bytes as that is the maximum we will see in the current fonts (upto 24)
    dup c@                  \ -- addr b1
    over 1+ c@              \ -- addr b1 b2
    rot 2+ c@               \ -- b1 b2 b3
    8 lshift -rot           \ -- b3<<8 b1 b2
    16 lshift -rot          \ -- b2<<16 b3<<8 b1
    24 lshift -rot          \ -- b1<<24 b2<<16 b3<<8
    or or                   \ -- b1b2b300
;

\ columns: 1 = # of params, 2 = command, 3 .. = params
create ILI9341_INIT_CMD
    4 ec, $EF ec, $03 ec, $80 ec, $02 ec,
    4 ec, ILI9341_PWCTRB    ec, $00 ec, $C1 ec, $30 ec,                    \ Pwr ctrl B
    5 ec, ILI9341_POSC      ec, $64 ec, $03 ec, $12 ec, $81 ec,              \ Pwr on seq. ctrl
    4 ec, ILI9341_DTCA      ec, $85 ec, $00 ec, $78 ec,                      \ Driver timing ctrl A
    6 ec, ILI9341_PWCTRA    ec, $39 ec, $2C ec, $00 ec, $34 ec, $02 ec,    \ Pwr ctrl A
    2 ec, ILI9341_PUMPRC    ec, $20 ec,                                    \ Pump ratio control
    3 ec, ILI9341_DTCB      ec, $00 ec, $00 ec,                              \ Driver timing ctrl B
    2 ec, ILI9341_PWCTR1    ec, $23 ec,                                    \ Pwr ctrl 1
    2 ec, ILI9341_PWCTR2    ec, $10 ec,                                    \ Pwr ctrl 2
    3 ec, ILI9341_VMCTR1    ec, $3E ec, $28 ec,                            \ VCOM ctrl 1
    2 ec, ILI9341_VMCTR2    ec, $86 ec,                                   \ VCOM ctrl 2
    2 ec, ILI9341_MADCTL    ec, $88 ec,                                    \ Memory access ctrl
    \ 2 ec, ILI9341_VSCRSADD  ec, $00 ec,                                  \ Vertical scrolling start address
    2 ec, ILI9341_PIXFMT    ec, $55 ec,                                    \ COLMOD: Pixel format
    4 ec, ILI9341_DFUNCTR   ec, $08 ec, $82 ec, $27 ec,
    2 ec, ILI9341_ENABLE3G  ec, $00 ec,                                  \ Enable 3 gamma ctrl
    2 ec, ILI9341_GAMMASET  ec, $01 ec,                                  \ Gamma curve selected
   16 ec, ILI9341_GMCTRP1   ec, $0F ec, $31 ec, $2B ec, $0C ec, $0E ec,
                                $08 ec, $4E ec, $F1 ec, $37 ec, $07 ec,
                                $10 ec, $03 ec, $0E ec, $09 ec, $00 ec,
   16 ec, ILI9341_GMCTRN1   ec, $00 ec, $0E ec, $14 ec, $03 ec, $11 ec,
                                $07 ec, $31 ec, $C1 ec, $48 ec, $08 ec,
                                $0F ec, $0C ec, $31 ec, $36 ec, $0F ec,
    3 ec, ILI9341_FRMCTR1   ec, $00 ec, $10  ec,                          \ Frame rate ctrl
    0 ec,  0 ec, ecalign \ terminate list


pin16 constant ILI9341_RST
pin17 constant ILI9341_DC
pin18 constant ILI9341_CS

: pin-res-high ILI9341_RST pin-high ;
: pin-res-low  ILI9341_RST pin-low ;
: pin-dc-high  ILI9341_DC pin-high ;
: pin-dc-low   ILI9341_DC pin-low ;
: pin-cs-high  ILI9341_CS pin-high ;
: pin-cs-low   ILI9341_CS pin-low ;

\ swap 16 bit nibbles
: h<> ( h -- h ) dup $FF and 8 lshift swap 8 rshift or ;

\ swap 32 bit nibbles
: <> ( d -- d ) dup $FFFF and h<> 16 lshift swap 16 rshift h<> or ;

4 buffer: cmd_buf
: ili9341-write-cmd ( cmd -- )
    cmd_buf c!
    pin-dc-low
    pin-cs-low
    1 cmd_buf spi1-write
    pin-cs-high
;

: ili9341-write-data ( data -- )
    cmd_buf c!
    pin-dc-high
    pin-cs-low
    1 cmd_buf spi1-write
    pin-cs-high
;

: ili9341-write-datan ( buf n -- )
    pin-dc-high
    pin-cs-low
    swap spi1-write
    pin-cs-high
;

: ili9341-write-data16n ( d16 n -- )
    pin-dc-high
    pin-cs-low
    swap spi1-write16
    pin-cs-high
;

: ili9341-init-commands
    ILI9341_SWRESET ili9341-write-cmd        \ software reset
    100 ms
    ILI9341_INIT_CMD
    begin
        dup c@ over 1+ c@                    \ -- addr n# cmd
        over 0= if
            2drop drop
            ILI9341_SLPOUT ili9341-write-cmd        \ Exit sleep
            100 ms
            ILI9341_DISPLAY_ON ili9341-write-cmd    \ Display on
            100 ms
            exit
        then
        ili9341-write-cmd                     \ -- addr n#
        swap 2+ swap                         \ -- addr+2 n#
        1-
        ?dup if                              \ n > 0
            2dup                             \ -- addr+2 n# addr+2 n#
            ili9341-write-datan
            +                                \ -- addr+n#
        then
    again
;

\ 8:MISO, 15:MOSI, 14:SCLK, 16:RST, 17:DC, 18:CS
: ili9341-init
    ILI9341_RST pin-output
    ILI9341_DC pin-output
    ILI9341_CS pin-output

    14 15 8 spi1-set-pins if ." invalid pins for SPI1" exit then
    cr ." Using pins: " 14 15 8 3 0 do dup . spi1-show-pin. loop cr

    40000000 spi1-init

    pin-res-high
    pin-cs-high
    pin-dc-low
    pin-res-low
    50 ms
    pin-res-high
    50 ms

    ili9341-init-commands
;

\ 0, 90, 180, 270
: ili9341-rotation ( rotation -- )
    case
        90 of $E8
                ILI9341_defWIDTH  ILI9341_HEIGHT !
                ILI9341_defHEIGHT ILI9341_WIDTH !
            endof
        180 of $88
                ILI9341_defWIDTH ILI9341_WIDTH !
                ILI9341_defHEIGHT  ILI9341_HEIGHT !
             endof
        270 of $28
                ILI9341_defWIDTH ILI9341_HEIGHT !
                ILI9341_defHEIGHT ILI9341_WIDTH !
            endof

        \ 0 and default
        $48
        ILI9341_defWIDTH ILI9341_WIDTH !
        ILI9341_defHEIGHT ILI9341_HEIGHT !
    endcase

    ILI9341_MADCTL ili9341-write-cmd
    ili9341-write-data
;

\ clamp x between low and high
: clamp ( x low high -- nx )
    dup 3 pick < if -rot 2drop exit then
    drop
    dup 2 pick > if nip exit then
    drop
;

: clamp-to-window-size ( xs xe ys ye -- nxs nxe nys nye )
    0 ILI9341_HEIGHT @ clamp 3 -roll
    0 ILI9341_HEIGHT @ clamp 3 -roll
    0 ILI9341_WIDTH @ clamp 3 -roll
    0 ILI9341_WIDTH @ clamp 3 -roll
;

\ %rrrrrggg gggbbbbb
: ili9341-rgb>565 ( r g b -- rgb )
    \ ((aR & 0xF8) << 8) | ((aG & 0xFC) << 3) | (aB >> 3)
    rot $F8 and 8 lshift rot $FC and 3 lshift or swap 3 rshift or
;

\ bkcolor|fgcolor
0 variable current_color
: set-color ( r g b -- ) ili9341-rgb>565 $0000FFFF and current_color h! ;
: set-bgcolor ( r g b -- ) ili9341-rgb>565 16 lshift current_color @ $0000FFFF and or current_color ! ;
: fg-color current_color @ $FFFF and ;
: bg-color current_color @ 16 rshift ;

4 buffer: data4-buf
: >hdata4-buf ( d # -- ) 2* data4-buf + swap h<> swap h! ;

: ili9341-set-window-location ( xs xe ys ye -- )
    ILI9341_SET_COLUMN ili9341-write-cmd
    2swap swap                                  \ -- ys ye xe xs
    0 >hdata4-buf                               \ xs
    1- 1 >hdata4-buf                            \ xe
    data4-buf 4 ili9341-write-datan
                                                \ -- ys ye
    ILI9341_SET_PAGE ili9341-write-cmd
    swap
    0 >hdata4-buf                                \ ys
    1- 1 >hdata4-buf                             \ ye
    data4-buf 4 ili9341-write-datan
;

\ takes ~20 ms
: ili9341-clearscreen ( r g b -- )
    ili9341-rgb>565
    0 ILI9341_WIDTH @ 0 ILI9341_HEIGHT @ ili9341-set-window-location
    ILI9341_WRITE_RAM ili9341-write-cmd ILI9341_SCREEN_SIZE ili9341-write-data16n
;

\ convert from a rectangle format to window format
: ili9341-rectangle ( x0 y0 width height -- xs xe ys ye )
    3 roll dup 3 roll +   \ -- y0 height x0 xe
    2swap over +            \ -- x0 xe y0 ye
;

\ fill rect with fg-color
: ili9341-fillrect ( xs xe ys ye -- )
    clamp-to-window-size    \ xs xe ys ye
    2over swap - 1+         \ xs xe ys ye xsize
    4 -roll                 \ xsize xs xe ys ye
    2dup swap - 1+          \ xsize xs xe ys ye ysize
    5 roll * 4 -roll            \ ysize*xsize xs xe ys ye
    ili9341-set-window-location  \ ysize*xsize
    ILI9341_WRITE_RAM ili9341-write-cmd
    fg-color swap ili9341-write-data16n
;

\ size of the buffer for the selected font
: char-cell-buf-size font-width font-height * 2* ;

\ NOTE this is currently the maximum size for Font24
\ max-font-width * max-font-height * 2
17 24 * 2* constant MAX_CHAR_CELL_BUF_SIZE
MAX_CHAR_CELL_BUF_SIZE buffer: char_cell_buf
\ convert the font into a cell of rgb of the fg_color
\ convert the row of pixels (x) into rgb starting at caddr
\ the bits are left aligned within the word
: charrow>rgb ( data32 row -- )
    swap
    font-width 0 do                                \ process width bits within that word
        dup i lshift $80000000 and
        if fg-color else bg-color then h<>         \ -- row data rgb
        2 pick font-width * 2* char_cell_buf + i 2* + h!
    loop
    2drop
;

: ili9341-char ( c x y -- )
    over font-width + swap            \ -- c xs xe y
    dup font-height +                  \ -- c xs xe ys ye
    \ clamp-to-window-size
    ili9341-set-window-location
    \ clear character cell
    char_cell_buf char-cell-buf-size 0 fill
    font-data                                   \ address of start of pixel data that is left aligned in a 32bit word
    font-height 0 do                            \ height rows of pixels
        dup i font-row                          \ row pixel data we fetch 32 bits but only need some of them so they are left aligned
        i charrow>rgb                           \ convert the row into rgb
    loop
    drop

    ILI9341_WRITE_RAM ili9341-write-cmd
    char_cell_buf char-cell-buf-size ili9341-write-datan
;

: ili9341-str ( saddr n x y -- )
    2swap                               \ -- x y saddr n
    0 do                                \ -- x y saddr
        dup i + c@                      \ -- x y saddr c
        2over ili9341-char               \ -- x y saddr
        rot font-width + -rot           \ increment the col
    loop
    2drop drop
;

: n>str s>d TUCK DABS <#  #S ROT SIGN  #> ;
: ili9341-printn ( x y n -- ) n>str 2swap ili9341-str ;

: rowcol>xy ( row col -- x y ) font-width * swap font-height 2* * swap ;

: ili9341-test1
    ili9341-init

    begin
        cycles
        0 0 0 ili9341-clearscreen
        cycles
        swap - ." took " . ." us" cr
        1000 ms
        255 255 255 ili9341-clearscreen 1000 ms
        255 0 0 ili9341-clearscreen 1000 ms
        0 255 0 ili9341-clearscreen 1000 ms
        0 0 255 ili9341-clearscreen 1000 ms
    key? until
;

: ili9341-test2
    ili9341-init
    0 0 0 ili9341-clearscreen
    240 0 do
        0 0 255 set-color
        0 i + 8 i +  0 i + 8 i + ili9341-fillrect
        50 ms
        \ clear it but use a different way of specifying the rectangle
        0 0 0 set-color
        0 i + 0 i + 8 8 ili9341-rectangle ili9341-fillrect
    8 +loop
;

: ili9341-test3
    ili9341-init
    0 0 0 ili9341-clearscreen
    0 0 0 set-bgcolor
    255 0 0 set-color
    \ Font16 set-font
    cr ." font width: " font-width . ." font height: " font-height . ." font stride: " font-stride . cr

    s" RED" 0 0 ili9341-str
    0 255 0 set-color
    s" GREEN" 0 font-height ili9341-str
    0 0 255 set-color
    s" BLUE" 0 font-height 2* ili9341-str
    255 255 255 set-color
    0 font-height 3 * 123456789 ili9341-printn
    255 0 0 set-color
    255 255 0 set-bgcolor
    s" YEL=RED" 0 font-height 4 * ili9341-str
;

: chars-per-line ILI9341_WIDTH @ font-width / ;
: lines-per-screen ILI9341_HEIGHT @ font-height / ;
0 variable y
0 variable x
: ili9341-test4
    ili9341-init
    0 y ! 0 x !
    255 255 255 set-color
    0 0 0 set-bgcolor

    \ space - ~
    0 0 0 ili9341-clearscreen
    95 0 do
        32 i + x @ y @ ili9341-char
        font-width x +!
        i 1+ chars-per-line mod 0= if font-height y +! 0 x ! then
        y @ ILI9341_HEIGHT @ >= if 0 y ! key 0 0 0 ili9341-clearscreen then
    loop
;

\ : hdump
\     font-height 0 do
\         cr
\         font-width 0 do
\             char_cell_buf i 2* + j font-width 2* * + h@ hex.
\         loop
\     loop
\ ;
