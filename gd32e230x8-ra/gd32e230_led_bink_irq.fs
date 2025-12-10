\ gd32e230_led_blink_irq.fs
\ LCSC GD32E230 Dev Board
\ PC13 --- R --- LED --- GND

require cortex-m/stk.fs
require gd32e23x/gpioc.fs

\ IO port clocks are enabled by Mecrisp-Stellaris

: gpio-init
    $00000000 GPIOC_OMODE !        \ Push-pull on port pin C13
    $04000000 GPIOC_CTL !          \ Output on port pin C13
;

: led-toggle
    \ $00002000 GPIOC_ODR xor!       \ Toggle port pin C13
    $00002000 GPIOC_TG !           \ GD32E230 has a toggle register!
;

: systick-init
    ['] led-toggle irq-systick !   \ Setup handler
    $00800000 STK_VAL !            \ Set start value
    $00800000 STK_LOAD !           \ Set reload value
    $00000007 STK_CTRL !           \ Start timer and enable interrupt
;

: start gpio-init systick-init ;
