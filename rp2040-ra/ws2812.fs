\ This is **not** an optimal WS2812 (aka NeoPixel) driver for the RP2040
\ instead it experimental code written to explore the MCU'ss very
\ flexible DMA engine and to some extend its PWM timers.
\ 
\ The DMA engine is used to playback a stream of precomputed duty cycle values
\ That just happen to control a WS2812 RGB LED. The timer is configured to pull
\ a new compare value from the DMA channel every time it reaches its top value.

\ Compile to RAM instead of flash since experimental code changes often.
compiletoram

\ Define the required peripheral base addresses.
$40014000 constant io-bank0
$40050000 constant pwm
$50000000 constant dma

\ Cortex-M0+ Internal Peripherals
$e0000000               constant ppb

    \ SysTick
    ppb $e010 +         constant stk-csr   \ SysTick Control and Status Register
    ppb $e014 +         constant stk-rvr   \ SysTick Reload Value Register
    ppb $e018 +         constant stk-cvr   \ SysTick Current Value Register
    ppb $e01c +         constant stk-calib \ SysTick Calibration Value Register
    
    \ NVIC
    ppb $e100 +         constant nvic-iser  \ Interrupt Set-Enable Register
    ppb $e180 +         constant nvic-icer  \ Interrupt Clear-Enable Register
    ppb $e200 +         constant nvic-ispr  \ Interrupt Set-Pending Register
    ppb $e280 +         constant nvic-icpr  \ Interrupt Clear-Pending Register
    ppb $e400 +         constant nvic-ipr   \ Interrupt Priority Registers
        nvic-ipr $00 +  constant nvic-ipr0  \ Interrupt Priority Register 0
        nvic-ipr $04 +  constant nvic-ipr1  \ Interrupt Priority Register 1
        nvic-ipr $08 +  constant nvic-ipr2  \ Interrupt Priority Register 2
        nvic-ipr $0c +  constant nvic-ipr3  \ Interrupt Priority Register 3
        nvic-ipr $10 +  constant nvic-ipr4  \ Interrupt Priority Register 4
        nvic-ipr $14 +  constant nvic-ipr5  \ Interrupt Priority Register 5
        nvic-ipr $18 +  constant nvic-ipr6  \ Interrupt Priority Register 6
        nvic-ipr $1c +  constant nvic-ipr7  \ Interrupt Priority Register 7
    ppb $ed00 +         constant cpuid      \ CPUID Base Register
    ppb $ed04 +         constant icsr       \ Interrupt Control and State Register
    ppb $ed08 +         constant vtor       \ VTOR Vector Table Offset Register
    ppb $ed0c +         constant aircr      \ Application Interrupt and Reset Control Register
    ppb $ed10 +         constant scr        \ System Control Register
    ppb $ed14 +         constant ccr        \ Configuration and Control Register

\ Define which GPIO pin is connected to the WS2812 LED (strip).
\ Driving a WS2812 LED's data input directly from a 3.3V GPIO
\ is outside the specification if the supply voltage for the LED is 5V.
\ Use a level shifter for reliable operation or drop the supply voltage of the first LED
\ enough to bring its logic high voltage down to below 3.3V. 
15 constant rgb-pin 



\ Append a whole string to the formatted output buffer instead of just single characters.
: holds< ( c-addr n -- ) over + swap ?do i c@ hold< loop ;

\ Helper words to interact with bitfields like those found in peripheral configurations.
\ Calls to these words will often be optimized away by constant folding.
: bit?        ( x n -- ? ) 31 swap - lshift 0< inline 2-foldable ;
: shifts>     ( h l -- < > ) swap negate 31 + tuck + 2-foldable ;
: >shifts     ( h l -- < > ) swap negate 31 + tuck + swap 2-foldable ;
: lrshift     ( x < > -- x ) -rot lshift swap rshift 3-foldable ;
: bits>       ( x h l -- x ) shifts> lrshift 3-foldable inline ;
: >bits       ( x h l -- x ) >shifts lrshift 3-foldable inline ;
: +bit        ( x n -- x ) bit or  2-foldable inline ;
: -bit        ( x n -- x ) bit bic 2-foldable inline ;

\ Writing peripheral configurations is already tedious and error prone enough.
\ This macro defines multiple words to interact with a single bit bitfield.
: bit: ( bit "name" -- )
    >r token ( bit name len )

    \ Define <name>? to test for the bit
    \ : <name>? ( x -- ? ) <bit#> bit? 1-foldable inline ;
    <# s" : " holds< 2dup holds< [char] ? hold< 0 0 #> evaluate
    r@ 0 <# #s #> evaluate
    s" bit? 1-foldable inline ;" evaluate
    
    \ Define +<name> to set the bit
    \ : +<name> ( x -- x' ) <bit#> bit or 1-foldable inline ;
    <# s" : +" holds< 2dup holds< 0 0 #> evaluate
    r@ 0 <# #s #> evaluate
    s" bit or 1-foldable inline ;" evaluate

    \ Define -<name> to clear the bit
    \ : -<name> ( x -- x' ) <bit#> bit bic 1-foldable inline ;
    <# s" : -" holds< 2dup holds< 0 0 #> evaluate
    r@ 0 <# #s #> evaluate
    s" bit bic 1-foldable inline ;" evaluate
    
    \ Define ><name> to write to the bit
    \ : ><name> ( ? -- x ) 0= 1+ <bit#> lshift 1-foldable inline ;
    <# s" : >" holds< 2dup holds< 0 0 #> evaluate
    s" 0= 1+" evaluate
    r@ 0 <# #s #> evaluate
    s" lshift 1-foldable inline ;" evaluate
    
    rdrop 2drop ;

\ This macro defines multiple words to interact with a multibit bitfield.
: bits: ( high low "name" -- )
    token ( high low name len )

    <# s" : " holds< 2dup holds< s" >>" holds< 0 0 #> evaluate
    2over s" bits> inline 1-foldable ;" evaluate

    <# s" : >>" holds< 2dup holds< 0 0 #> evaluate
    2over s" >bits inline 1-foldable ;" evaluate
    2drop 2drop ;



\ Returns the bitmask and address configure a GPIO pin as output.
: >output ( pin -- mask addr ) bit gpio-oe >sio-bis 1-foldable ;

\ Returns the bitmask and address configure a GPIO pin as input. 
: >input  ( pin -- mask addr ) bit gpio-oe >sio-bic 1-foldable ;

\ Get the address of a DMA channel by index.
\ Only DMA channels [0, 11] are implemented on the RP2040.
: >dma-chan ( index -- dma-chan ) 28 lshift 22 rshift dma + inline 1-foldable ;
: dma-chan> ( dma-chan -- index ) 22 lshift 28 rshift inline 1-foldable ;

\ Name the DMA channels.
\ 00 >dma-chan constant ch00
\ 01 >dma-chan constant ch01
\ 02 >dma-chan constant ch02
\ 03 >dma-chan constant ch03
\ 04 >dma-chan constant ch04
\ 05 >dma-chan constant ch05
\ 06 >dma-chan constant ch06
\ 07 >dma-chan constant ch07
\ 08 >dma-chan constant ch08
\ 09 >dma-chan constant ch09
\ 10 >dma-chan constant ch10
\ 11 >dma-chan constant ch11
\
\ Not implemented in hardware, used as error value.
\ 12 >dma-chan constant ch12

\ All 12 channels are created equal in hardware
\ this means unlike other MCUs on the RP2040 they can be dynamically allocated.
\ The allocator uses a bitmap to track available channels.
\ 
\ Bit indices correspond to channel numbers.
\   * 1 = available for allocation
\   * 0 = unavailable for allocation
here 12 bit 1- h, constant dma-allot

\ Convert a DMA channel address to the single bit bitmask tracking its allocation status.
: dma-chan>bit ( ch -- mask )
    22 lshift 28 rshift ( ch#   ) \ Extract the channel number from a pointer.
    bit                 ( mask  ) \ Convert the channel number into a channel bitmask.
    20 lshift 20 rshift ( mask' ) \ Ignore attempts to allocate channels unimplemented on the RP2040.
    1-foldable ;

\ Test if a given DMA channel is available for allocation
: dma-avail? ( ch -- free? ) dma-chan>bit dma-allot hbit@ ;

\ There are 12 channels implemented in hardware.
\ There is no protection against taking an already allocated channel.
\ HINT: Use dma-alloc to allocate the next free DMA channel instead.
: dma-take ( ch --   ) dma-chan>bit dma-allot hbic! ;

\ Free a specific DMA channel for reuse.
\ There is neither protection against freeing active DMA channels
\ nor against (double) freeing unallocated channels.
\ NOTE: It's the users responsibility to stop DMA channels before releasing them.
: dma-free ( ch --   ) dma-chan>bit dma-allot hbis! ;

\ Dump the DMA channel allocation bitmap.
\ Came in useful debugging the allocator.
\ : dma-allot. ( -- )
\     12 >dma-chan 00 >dma-chan DO 
\         cr ." DMA channel " i dma-chan> .
\         i dma-avail? IF
\             ." is free." 
\         ELSE
\             ." is USED." 
\         THEN
\     01 >dma-chan 00 >dma-chan - +LOOP cr ;

\ Find the next free DMA channel available for allocation.
\ Possible return values:
\    * a free channel and true
\    * reserved unimplemented channel and false
: dma-find ( -- dma-chan ? )
    false                             \ Start assuming there is no available DMA channel.
    12 >dma-chan 00 >dma-chan DO      \ Try all 12 DMA channels implemented on the RP2040.
        i dma-avail? IF               \ Have we found one available for allocation?
            drop i true UNLOOP EXIT   \ If so return it (and success) 
        THEN                          \ 
    01 >dma-chan 00 >dma-chan - +LOOP \ Try the next DMA channel.
    12 >dma-chan swap ;               \ All DMA channels are allocated return an unimplemented DMA channel and failure. 

\ Attempt to allocate the next free DMA channel.
: dma-alloc ( -- dma-chan ? ) dma-find dup if over dma-take then ;

\ Unused constants that for future experiments.
\ 8 0 * 4 0 * + 0 + constant dreq-pio0-tx0
\ 8 0 * 4 0 * + 1 + constant dreq-pio0-tx1
\ 8 0 * 4 0 * + 2 + constant dreq-pio0-tx2
\ 8 0 * 4 0 * + 3 + constant dreq-pio0-tx3
\ 
\ 8 0 * 4 1 * + 0 + constant dreq-pio0-rx0
\ 8 0 * 4 1 * + 1 + constant dreq-pio0-rx1
\ 8 0 * 4 1 * + 2 + constant dreq-pio0-rx2
\ 8 0 * 4 1 * + 3 + constant dreq-pio0-rx3
\ 
\ 8 1 * 4 0 * + 0 + constant dreq-pio1-tx0
\ 8 1 * 4 0 * + 1 + constant dreq-pio1-tx1
\ 8 1 * 4 0 * + 2 + constant dreq-pio1-tx2
\ 8 1 * 4 0 * + 3 + constant dreq-pio1-tx3
\ 
\ 8 1 * 4 1 * + 0 + constant dreq-pio1-rx0
\ 8 1 * 4 1 * + 1 + constant dreq-pio1-rx1
\ 8 1 * 4 1 * + 2 + constant dreq-pio1-rx2
\ 8 1 * 4 1 * + 3 + constant dreq-pio1-rx3

\ The DMA data requests signal numbers for the 8 PWM slices start 24 going upward.
24 constant dreq-pwm-wrap
: slice>dreq ( slice# -- dreq# ) dreq-pwm-wrap + 1-foldable inline ;
\ dreq-pwm-wrap 0 + constant dreq-pwm-wrap0
\ dreq-pwm-wrap 1 + constant dreq-pwm-wrap1
\ dreq-pwm-wrap 2 + constant dreq-pwm-wrap2
\ dreq-pwm-wrap 3 + constant dreq-pwm-wrap3
\ dreq-pwm-wrap 4 + constant dreq-pwm-wrap4
\ dreq-pwm-wrap 5 + constant dreq-pwm-wrap5
\ dreq-pwm-wrap 6 + constant dreq-pwm-wrap6
\ dreq-pwm-wrap 7 + constant dreq-pwm-wrap7

\ DMA channels are configured through four memory registers per channel (read, write, count, control).
\ The DMA hardware provides four sets of aliases optimized for different usecases:
\                   _________ _______ _______ _________
\ base = $50000000 |    +$00 |  +$04 |  +$08 |    +$0C |
\ +----------------+---------+-------+-------+---------+
\ | alias 0 | +$00 |    read | write | count | control |
\ | alias 1 | +$10 | control |  read | write |   count |
\ | alias 2 | +$20 | control | count |  read |   write |
\ | alias 3 | +$30 | control | write | count |    read |
\ +----------------+---------+-------+-------+---------+
\ 
\ Pointers to DMA channel registers can thus be viewed as containing these bitfields:
\ 
\          31:10 |     9:6 |   5:4 |   3:2 | 1:0  
\ +--------------+---------+-------+-------+-----+
\ | base address | channel | alias | field | 0 0 |
\ +--------------+---------+-------+-------+-----+
\ 
\ 
\ The following helpers allow taking a prefix (base address, channel, alias) and setting the field offset for the
\ selected alias. They're a bit too cute, but as pure functions they can be folded away by the compiler
\ e.g. ch00 >alias0 >write
: >read  ( ch -- ch' ) 4 rshift 4 lshift dup 26 lshift 28 rshift or 1-foldable inline ;
: >write ( ch -- ch' ) 4 rshift 4 lshift dup 26 lshift 28 rshift 4 + dup 4 rshift 1 lshift rshift or 1-foldable ;
: >count ( ch -- ch' ) 4 rshift 4 lshift dup 26 lshift 29 rshift %10011110 swap rshift 30 lshift 28 rshift or 1-foldable ;
: >ctrl  ( ch -- ch' ) 4 rshift 2 lshift dup 28 lshift 0= 30 rshift or 2 lshift 1-foldable ;

\ Convert from any alias to a specific alias using a 32 bit lookup table indexed with variable width shifts.
: >alias ( ch perm -- ch )
    \ Extract the alias and field (read, write, count, control) bits from the DMA channel field address.
    over 26 lshift 27 rshift ( ch perm )

    \ Index into the permutation using a right shift (the result is in the lowest two bits)
    rshift   ( ch map[alias,field] )

    \ Clear the upper 30 bits containing the higher bits of the permutation
    30 lshift 28 rshift               ( ch map[alias,field] )
    
    \ Replace the field leaving the alias empty to easy insertion by addition.
    swap 6 rshift 6 lshift or 2-foldable ; ( ch' )

\ Translate a DMA channel register address (read, write, count, control) to alias 0.
: >alias0 ( ch -- ch' )
\    |alias3||alias2||alias1||alias0| RR (00) = read addr  , WW (10) = write addr
\    RRTTWWCCWWRRTTCCTTWWRRCCCCTTWWRR TT (10) = trans count, CC (11) = control
    %00100111010010111001001111100100 >alias $00 + 1-foldable ;

\ Translate a DMA channel register address (read, write, count, control) to alias 1.
: >alias1 ( ch -- ch' )
\    |alias3||alias2||alias1||alias0| RR (01) = read addr  , WW (10) = write addr
\    RRTTWWCCWWRRTTCCTTWWRRCCCCTTWWRR TT (11) = trans count, CC (00) = control
    %01111000100111001110010000111001 >alias $10 + 1-foldable ;

\ Translate a DMA channel register address (read, write, count, control) to alias 2.
: >alias2 ( ch -- ch' )
\    |alias3||alias2||alias1||alias0| RR (10) = read addr  , WW (11) = write addr
\    RRTTWWCCWWRRTTCCTTWWRRCCCCTTWWRR TT (01) = trans count, CC (00) = control
    %10011100111001000111100000011110 >alias $20 + 1-foldable ;

\ Translate a DMA channel register address (read, write, count, control) to alias 3.
: >alias3 ( ch -- ch' )
\    |alias3||alias2||alias1||alias0| RR (11) = read addr  , WW (01) = write addr
\    RRTTWWCCWWRRTTCCTTWWRRCCCCTTWWRR TT (10) = trans count, CC (00) = control
    %11100100011110001001110000100111 >alias $30 + 1-foldable ;

\ Each of the 30 GPIO pins the RP2040 offers can be routed to
\ one function/peripheral at a time.
 1 constant func-spi  \ Route GPIO pin to a SPI peripheral
 2 constant func-uart \ Route GPIO pin to an UART peripheral
 3 constant func-i2c  \ Route GPIO pin to an I2C peripheral
 4 constant func-pwm  \ Route GPIO pin to a PWM block
 5 constant func-sio  \ Route GPIO pin to the single cyle I/O block = CPU control
 6 constant func-pio0 \ Route GPIO pin to PIO block 0
 7 constant func-pio1 \ Route GPIO pin to PIO block 1
 9 constant func-usb  \ Route GPIO pin to the USB peripheral
31 constant func-none \ Disconnect GPIO pin (this is the reset value)

: 'f ( -- flags ) token find nip ;

: pin>status ( pin# -- status ) 27 lshift 24 rshift io-bank0       + 1-foldable ;
: pin>ctrl   ( pin# -- ctrl   ) 27 lshift 24 rshift io-bank0 cell+ + 1-foldable ;
: >func-sel  ( x    -- func   ) 27 lshift 27 rshift 1-foldable inline ;

\ Returns the currently active function of a given GPIO pin. 
: pin-func@  ( pin# -- func   )
    pin>ctrl @ >func-sel exit
    [ $1000 setflags 3 h, \ Attach an inline cache to the word.
        ' pin>ctrl  , 'f pin>ctrl  h,
        ' @         , 'f @         h,
        ' >func-sel , 'f >func-sel h,
    ] ;

: (pin-func!) ( func &ctrl -- ) 
    tuck @                    ( &ctrl func ctrl ) \ Load the old value,
    xor >func-sel             ( &ctrl diff      ) \ exploit that XOR is self-inverse and commutative, and
    swap dup 18 rshift or !   (                 ) \ extract XOR alias from known upper bits the GPIO control address
    inline                  ; (                 ) \ to toggle the bits that conflict with the desired state.

: pin-func! ( func pin# -- )
    pin>ctrl (pin-func!) exit
    [ $1000 setflags 2 h,
        ' pin>ctrl    , 'f pin>ctrl    h,
        ' (pin-func!) , 'f (pin-func!) h,
    ] ;

\ Atomically route a given GPIO pin to a given peripheral/function.
\ This code reads the current memory mapped register value,
\ computes the bits that conflict with the requested configuration,
\ and toggles them by writing it to the xor alias address of the register.
\ : pin-func! ( func pin# -- )
\     27 lshift 24 rshift 4 +       ( func offset      ) \ Map GPIO pin number to index into IOBANK0 registers
\     tuck io-bank0 + @ xor $1f and ( offset func-diff ) \ Compute difference between function and register, mask it,
\     swap io-bank0 >io-xor + ! ;   (                  ) \ exploit that XOR is self-inverse and commutative.

\ DMA control and status register:
   31  bit: ahb-err
   30  bit: read-err
   29  bit: write-err
   24  bit: busy
   23  bit: sniff
   22  bit: bswap
   21  bit: quiet
20 15 bits: treq-sel
14 11 bits: chain-to
   10  bit: ring-sel
09 06 bits: ring-size
   05  bit: incr-write
   04  bit: incr-read
03 02 bits: data-size
   01  bit: high-prio
   00  bit: enable



\ Calculate the offset of a PWM slice from the PWM base address
: >pwm ( index -- pw>>treq-selm-slice )
    29 lshift                   ( index<<29          ) \ Mask the (too) high bits by shifting them out.
    dup 25 rshift               ( index<<29 16*index ) \ Perform a partial shift back to calculate 16*index
    swap 27 rshift              ( 16*index   4*index ) \ Perform a second partial shift back to calculate 4*index
    + pwm + 1-foldable inline ; ( 20*index+pwm       ) \ Use the distributive law: (16*index)+(4*index) = 20*index

\ Map from GPIO pin numbers to PWM timer slices and channels.
: pin>slice ( pin# -- slice# ) 28 lshift 29 rshift 1-foldable inline ;
: pin>pwm ( pin# -- ch# ) pin>slice >pwm 1-foldable inline ;

\ These words add the offsets of configuration registers for a PWM slice relative to its base address.
: +csr ( pwm-slice -- pwm-csr ) $00 + inline 0-foldable ; \ It's just a NOP and those can always be folded
: +div ( pwm-slice -- pwm-div ) $04 + inline 1-foldable ;
: +ctr ( pwm-slice -- pwm-ctr ) $08 + inline 1-foldable ;
: +cc  ( pwm-slice -- pwm-cc  ) $0C + inline 1-foldable ;
: +top ( pwm-slice -- pwm-top ) $10 + inline 1-foldable ;

\ Not needed if the Mecrisp Stellaris kernel supports the uxth instruction directly.
\ : uxth ( x -- x' ) [ $b2b6 h, ] inline 1-foldable ;

\ Set the counter compare register for the A channel of a PWM slice
: pwm-a! ( cc-a pwm -- )
    +cc swap uxth swap ( cc-a cc )
    tuck @ xor uxth    ( cc diff ) \ find out which bits differ between the old and new counter compare value
    swap >io-xor ! ;   (         ) \ Apply the difference to channel A of the counter compare register

\ Set the counter compare register for the B channel of a PWM slice
: pwm-b! ( cc-b pwm -- )
    +cc ( cc-b cc )
    tuck @ 16 rshift xor 16 lshift ( &cc diff )
    swap >io-xor ! ;

\ Set the PWM timer top value by GPIO pin number.
: pin-top! ( top pin# -- )
    pin>pwm +top ! ;

\ Set the PWM timer compare value by GPIO pin.
: pin-cc! ( cc pin# -- )
    dup pin>pwm swap ( cc pwm pin# )
    31 lshift 0< if pwm-b! else pwm-a! then ;

: io-bit! ( ? addr bit# -- ) bit rot 0= negate %10 + 12 lshift rot + ! ;
    
: pwm-on! ( ? pin -- )
    pin>pwm +csr 0 io-bit! ;

\ Calculate how many system clock cycles the PWM wrapping period should last
1200               ( ns/bit )            \ Each in the NeoPixel protocol is 1200ns long
1000 1000 * 1000 * ( ns/bit ns/s )       \ There are 10^9 nanoseconds to a second
 125 1000 * 1000 * ( ns/bit ns/s f_sys ) \ The PLL driving the system clock runs at 125MHz
/ / constant rgb-cycles

: pwm-init ( -- )
    rgb-cycles rgb-pin pin-top!    \ Set the PWM slice to wrap every 150 cycles (150 / 125MHz = 150 * 8ns = 1.2us)
    0          rgb-pin pin-cc!     \ Set the PWM duty cycle to 0%
    true       rgb-pin pwm-on!     \ Enable PWM slice
    func-pwm   rgb-pin pin-func! ; \ Connect GPIO pin to the PWM slice

\ Allocate a DMA channel for the RGB LED(s)
\ WARNING: Allocation failures are silently ignored (none are expected at load time).
dma-alloc drop constant rgb-chan
create rgb-buffer 0-foldable
here
    $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , \ %11111111
    $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , \ %11111111
    $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , $00630000 , \ %11111111

    $00310000 , $00630000 , $00310000 , $00630000 , $00310000 , $00630000 , $00310000 , $00630000 , \ %01010101
    $00630000 , $00630000 , $00630000 , $00630000 , $00310000 , $00310000 , $00310000 , $00310000 , \ %11110000
    $00310000 , $00310000 , $00310000 , $00310000 , $00630000 , $00630000 , $00630000 , $00630000 , \ %00001111

    $00310000 , $00000000 ,
here - negate constant rgb-size

\ Convert a 24 bit pixel encoded as zero extended GRB (not RGB) value to 24 PWM compare values.
: rgb>pwm ( 0grb addr -- )
    swap 8 lshift swap                                \ Left align the 24 bit pixel in the 32 bit register
    24 cells over + swap do                           \ Loop over all PWM timer compare values
        dup 31 rshift $63 $31 - * $31 + 16 lshift i ! \ Expand highest remaining bit into a PWM timer compare value
        1 lshift                                      \ Shift the next bit up into the MB
    1 cells +loop drop ;

\ Print 24 PWM compare values.
: pwm. ( addr -- )
    cr 24 cells over + swap do
        i @ 16 rshift .
    1 cells +loop ;



\ Constants for DMA channel configuration.
%00 constant dma-byte
%01 constant dma-half
%10 constant dma-word

\ Fancy macro to generate a word that configures a DMA channel as about quickly as possible.
: dma-init: ( read write count control chan "name" -- )
    (create)
        $B440 h, \ PUSH {r6}
        $A602 h, \ ADR  r6, #8
        $CE4F h, \ LDM  r6, {r0-r3, r6}
        $C60F h, \ STM  r6!, {r0-r3}
        $BC40 h, \ POP  {r6}
        $4770 h, \ BX   lr
    align here                      ( read write count ctrl chan here ) \ Ensure alignment, save start of allocation
    5 cells allot                   ( read write count ctrl chan here ) \ Allocate 5 cells
    tuck 4 cells offset!            ( read write count ctrl here )      \ Store DMA channel address
    tuck 3 cells offset!            ( read write count here )           \ Store DMA control flags
    tuck 2 cells offset!            ( read write here )                 \ Store DMA transfer count
    tuck 1 cells offset!            ( read here )                       \ Store DMA write address
         0 cells offset! smudge ;   ( )                                 \ Store DMA read address and finalize word



\ Use the abstractions to configure a DMA channel to drive a single WS2812 LED.
rgb-buffer                                 ( read                  ) \ Set DMA channel read address
rgb-pin pin>pwm +cc                        ( read write            ) \ Set DMA channel write address
rgb-size 4 /                               ( read write count      ) \ Set DMA channel transfer size (counted in words)
rgb-pin pin>slice slice>dreq >>treq-sel    ( read write count ctrl ) \ Pick DMA transfer request source (the PWM slice)
rgb-chan dma-chan>           >>chain-to  + ( read write count ctrl ) \ Don't chain to any other DMA channel
dma-word                     >>data-size + ( read write count ctrl ) \ Transfer 32 bit words
+incr-read +enable                         ( read write connt ctrl ) \ Increment source addresses, enable DMA channel
rgb-chan dma-init: dma-init

\ Set the color of the single WS2812 LED.
: rgb! ( 0grb -- ) rgb-buffer rgb>pwm dma-init ;
