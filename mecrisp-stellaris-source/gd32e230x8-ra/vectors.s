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

@ -----------------------------------------------------------------------------
@ Interruptvektortabelle
@ -----------------------------------------------------------------------------

.include "../common/vectors-common.s"

@ Special interrupt handlers for GD32E230:

.word nullhandler + 1           @ IRQ  0: WWDGT interrupt
.word irq_vektor_lvd + 1        @ IRQ  1: LVD from EXTI interrupt
.word nullhandler + 1           @ IRQ  2: RTC global interrupt
.word nullhandler + 1           @ IRQ  3: FMC global interrupt
.word nullhandler + 1           @ IRQ  4: RCU global interrupt
.word irq_vektor_exti0_1 + 1    @ IRQ  5: EXTI line 0-1 interrupt
.word irq_vektor_exti2_3 + 1    @ IRQ  6: EXTI line 2-3 interrupt
.word irq_vektor_exti4_15 + 1   @ IRQ  7: EXTI line 4-15 interrupt
.word nullhandler + 1           @ IRQ  8: Reserved
.word irq_vektor_dma_ch0 + 1    @ IRQ  9: DMA channel 0 global interrupt
.word irq_vektor_dma_ch1_2 + 1  @ IRQ 10: DMA channel 1-2 global interrupt
.word irq_vektor_dma_ch3_4 + 1  @ IRQ 11: DMA channel 3-4 global interrupt
.word irq_vektor_adc_cmp + 1    @ IRQ 12: ADC and CMP interrupt
.word irq_vektor_tim0_up + 1    @ IRQ 13: TIMER0 break, update, trigger and commutation interrupts
.word irq_vektor_tim0_cc + 1    @ IRQ 14: TIMER0 capture compare interrupt
.word nullhandler + 1           @ IRQ 15: Reserved
.word irq_vektor_tim2 + 1       @ IRQ 16: TIMER2 global interrupt
.word irq_vektor_tim5 + 1       @ IRQ 17: TIMER5 interrupt
.word nullhandler + 1           @ IRQ 18: Reserved
.word irq_vektor_tim13 + 1      @ IRQ 19: TIMER13 global interrupt
.word irq_vektor_tim14 + 1      @ IRQ 20: TIMER14 global interrupt
.word irq_vektor_tim15 + 1      @ IRQ 21: TIMER15 global interrupt
.word irq_vektor_tim16 + 1      @ IRQ 22: TIMER16 global interrupt
.word irq_vektor_i2c0_evt + 1   @ IRQ 23: I2C0 event interrupt
.word irq_vektor_i2c1_evt + 1   @ IRQ 24: I2C1 event interrupt
.word irq_vektor_spi0 + 1       @ IRQ 25: SPI0 global interrupt
.word irq_vektor_spi1 + 1       @ IRQ 26: SPI1 global interrupt
.word irq_vektor_usart1 + 1     @ IRQ 27: USART0 global interrupt
.word irq_vektor_usart2 + 1     @ IRQ 28: USART1 global interrupt
.word nullhandler + 1           @ IRQ 29: Reserved
.word nullhandler + 1           @ IRQ 30: Reserved
.word nullhandler + 1           @ IRQ 31: Reserved
.word irq_vektor_i2c0_err + 1   @ IRQ 32: I2C0 error interrupt
.word nullhandler + 1           @ IRQ 33: Reserved
.word irq_vektor_i2c1_err + 1   @ IRQ 34: I2C1 error interrupt
@ -----------------------------------------------------------------------------
