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

@ Routinen für die Interrupthandler, die zur Laufzeit neu gesetzt werden können.
@ Code for interrupt handlers that are exchangeable on the fly

@------------------------------------------------------------------------------
@ Alle Interrupthandler funktionieren gleich und werden komfortabel mit einem Makro erzeugt:
@ All interrupt handlers work the same way and are generated with a macro:
@------------------------------------------------------------------------------

interrupt lvd        @ IRQ  1: LVD from EXTI interrupt
interrupt exti0_1    @ IRQ  5: EXTI line 0-1 interrupt
interrupt exti2_3    @ IRQ  6: EXTI line 2-3 interrupt
interrupt exti4_15   @ IRQ  7: EXTI line 4-15 interrupt
interrupt dma_ch0    @ IRQ  9: DMA channel 0 global interrupt
interrupt dma_ch1_2  @ IRQ 10: DMA channel 1-2 global interrupt
interrupt dma_ch3_4  @ IRQ 11: DMA channel 3-4 global interrupt
interrupt adc_cmp    @ IRQ 12: ADC and CMP interrupt
interrupt tim0_up    @ IRQ 13: TIMER0 break, update, trigger and commutation interrupts
interrupt tim0_cc    @ IRQ 14: TIMER0 capture compare interrupt
interrupt tim2       @ IRQ 16: TIMER2 global interrupt
interrupt tim5       @ IRQ 17: TIMER5 interrupt
interrupt tim13      @ IRQ 19: TIMER13 global interrupt
interrupt tim14      @ IRQ 20: TIMER14 global interrupt
interrupt tim15      @ IRQ 21: TIMER15 global interrupt
interrupt tim16      @ IRQ 22: TIMER16 global interrupt
interrupt i2c0_evt   @ IRQ 23: I2C0 event interrupt
interrupt i2c1_evt   @ IRQ 24: I2C1 event interrupt
interrupt spi0       @ IRQ 25: SPI0 global interrupt
interrupt spi1       @ IRQ 26: SPI1 global interrupt
interrupt usart1     @ IRQ 27: USART0 global interrupt
interrupt usart2     @ IRQ 28: USART1 global interrupt
interrupt i2c0_err   @ IRQ 32: I2C0 error interrupt
interrupt i2c1_err   @ IRQ 33: Reserved

.ltorg
@------------------------------------------------------------------------------
