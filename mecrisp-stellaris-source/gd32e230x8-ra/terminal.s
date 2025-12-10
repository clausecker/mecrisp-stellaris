@
@    Mecrisp-Stellaris - A native code Forth implementation for ARM-Cortex M microcontrollers
@    Copyright (C) 2013  Matthias Koch
@
@    This program is free software: you can redistribute it and/or modify
@    it under the terms of the GNU General Public License as published by
@    the Free Software Foundation, either version 3 of the License, or
@    (at your option) any later version.
@
@    This program is distributed in the hope that it will be useful,
@    but WITHOUT ANY WARRANTY; without even the implied warranty of
@    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
@    GNU General Public License for more details.
@
@    You should have received a copy of the GNU General Public License
@    along with this program.  If not, see <http://www.gnu.org/licenses/>.
@

@ Terminalroutinen
@ Terminal code and initialisations.
@ Porting: Rewrite this !
@
@ LCSC GD32E230 Dev Board (https://oshwlab.com/lckfb-team/lcsc-gd32e230-dev-board)
@ The on-board USB-to-serial converter (CH340) is connected to USART0
@ (PA9 = TX, PA10 = RX).

   .equ GPIOA_BASE      ,   0x48000000
   .equ GPIOA_CTL       ,   GPIOA_BASE + 0x00
   .equ GPIOA_OMODE     ,   GPIOA_BASE + 0x04
   .equ GPIOA_OSPD      ,   GPIOA_BASE + 0x08
   .equ GPIOA_PUD       ,   GPIOA_BASE + 0x0C
   .equ GPIOA_ISTAT     ,   GPIOA_BASE + 0x10
   .equ GPIOA_OCTL      ,   GPIOA_BASE + 0x14
   .equ GPIOA_BOP       ,   GPIOA_BASE + 0x18
   .equ GPIOA_LOCK      ,   GPIOA_BASE + 0x1C
   .equ GPIOA_AFSEL0    ,   GPIOA_BASE + 0x20
   .equ GPIOA_AFSEL1    ,   GPIOA_BASE + 0x24
   .equ GPIOA_BC        ,   GPIOA_BASE + 0x28
   .equ GPIOA_TG        ,   GPIOA_BASE + 0x2C

   @ Reset and clock unit registers

   .equ RCU_BASE        ,   0x40021000
   .equ RCU_AHBEN       ,   RCU_BASE + 0x14
   .equ RCU_APB2EN      ,   RCU_BASE + 0x18
   .equ RCU_APB1EN      ,   RCU_BASE + 0x1C
	
   @ USART registers

   .equ USART0_BASE     ,   0x40013800
   .equ USART0_CTL0     ,   USART0_BASE + 0x00
   .equ USART0_CTL1     ,   USART0_BASE + 0x04
   .equ USART0_CTL2     ,   USART0_BASE + 0x08
   .equ USART0_BAUD     ,   USART0_BASE + 0x0C
   .equ USART0_GP       ,   USART0_BASE + 0x10
   .equ USART0_RT       ,   USART0_BASE + 0x14
   .equ USART0_CMD      ,   USART0_BASE + 0x18
   .equ USART0_STAT     ,   USART0_BASE + 0x1C
   .equ USART0_INTC     ,   USART0_BASE + 0x20
   .equ USART0_RDATA    ,   USART0_BASE + 0x24
   .equ USART0_TDATA    ,   USART0_BASE + 0x28
   .equ USART0_CHC      ,   USART0_BASE + 0xC0
   .equ USART0_RFCS     ,   USART0_BASE + 0xD0
	
   @ Flags for USARTx_STAT register:
   .equ TBE             ,   BIT7
   .equ TC              ,   BIT6
   .equ RBNE            ,   BIT5

@ -----------------------------------------------------------------------------
uart_init:
@ -----------------------------------------------------------------------------

   @ Turn on the clocks for all GPIOs.
   ldr r1, =RCU_AHBEN
   ldr r0, =BIT17 + BIT18 + BIT19 + BIT20 + BIT22   @ PAEN | PBEN | PCEN | PDEN | PFEN
   str r0, [r1]

   @ Turn on the clock for USART0.
   ldr r1, =RCU_APB2EN
   ldr r0, =BIT14   @ USART0EN
   str r0, [r1]

   @ Set PORTA pins 9 and 10 in alternate function mode.
   @ Keep PORTA pins 13 and 14 in alternate function mode.
   ldr r1, =GPIOA_CTL
   ldr r0, =0x28280000   @ AF mode for PA9, PA10, PA13 and PA14
   str r0, [r1]

   @ Set alternate function 1 to enable USART0 TX and RX pins on port A.
   @ Keep alternate function 0 to enable SWDIO and SWCLK pins on port A.
   ldr r1, =GPIOA_AFSEL1
   ldr r0, =0x00000110
   str r0, [r1]

   @ Configure baud rate.
   ldr r1, =USART0_BAUD
   movs r0, #0x46  @ 115200 bps
   str r0, [r1]

   @ Some RISC-V ports use two stop bits, so we should do the same here.
   ldr r1, =USART0_CTL1
   ldr r0, =0x00002000
   str r0, [r1]

   @ Enable the USART, receiver and transmitter
   ldr r1, =USART0_CTL0
   ldr r0, =BIT3 + BIT2 + BIT0   @ TEN | REN | REN
   str r0, [r1]

   bx lr

.include "../common/terminalhooks.s"

@ -----------------------------------------------------------------------------
  Wortbirne Flag_visible, "serial-emit"
serial_emit: @ ( c -- ) Emit one character
@ -----------------------------------------------------------------------------
   push {lr}

1: bl serial_qemit
   cmp tos, #0
   drop
   beq 1b

   ldr r2, =USART0_TDATA
   strb tos, [r2]         @ Output the character
   drop

   pop {pc}

@ -----------------------------------------------------------------------------
  Wortbirne Flag_visible, "serial-key"
serial_key: @ ( -- c ) Receive one character
@ -----------------------------------------------------------------------------
   push {lr}

1: bl serial_qkey
   cmp tos, #0
   drop
   beq 1b

   pushdatos
   ldr r2, =USART0_RDATA
   ldrb tos, [r2]         @ Fetch the character

   pop {pc}

@ -----------------------------------------------------------------------------
  Wortbirne Flag_visible, "serial-emit?"
serial_qemit:  @ ( -- ? ) Ready to send a character ?
@ -----------------------------------------------------------------------------
   push {lr}
   bl pause
   movs r2, #TBE
   b.n serial_qkey_intern

@ -----------------------------------------------------------------------------
  Wortbirne Flag_visible, "serial-key?"
serial_qkey:  @ ( -- ? ) Is there a key press ?
@ -----------------------------------------------------------------------------
   push {lr}
   bl pause
   movs r2, #RBNE

serial_qkey_intern:
   pushdaconst 0  @ False Flag
   ldr r0, =USART0_STAT
   ldr r1, [r0]     @ Fetch status
   ands r1, r2
   beq 1f
   mvns tos, tos @ True Flag

1: pop {pc}

  .ltorg @ Hier werden viele spezielle Hardwarestellenkonstanten gebraucht, schreibe sie gleich !
