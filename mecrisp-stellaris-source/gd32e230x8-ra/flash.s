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

@ Write and Erase Flash in GD32E230.
@ Porting: Rewrite this ! You need hflash! and - as far as possible - cflash!

.equ FMC_BASE, 0x40022000  @ Flash memory controller
.equ FMC_WS,     FMC_BASE + 0x00  @ Wait state register
.equ FMC_KEY,    FMC_BASE + 0x04  @ Unlock key register
.equ FMC_OBKEY,  FMC_BASE + 0x08  @ Option byte unlock key register
.equ FMC_STAT,   FMC_BASE + 0x0C  @ Status register
.equ FMC_CTL,    FMC_BASE + 0x10  @ Control register
.equ FMC_ADDR,   FMC_BASE + 0x14  @ Address register
.equ FMC_OBSTAT, FMC_BASE + 0x1C  @ Option byte status register

@ -----------------------------------------------------------------------------
  Wortbirne Flag_visible, "flash!" @ ( x Addr -- )
  @ Schreibt an die auf 4 gerade Adresse in den Flash.
flashkomma:
@ -----------------------------------------------------------------------------
  popda r0  @ address
  popda r1  @ data

  @ Make sure not to overwrite the Forth core.
  ldr r3, =Kernschutzadresse
  cmp r0, r3
  blo 3f

  @ Check whether the address is inside the flash.
  ldr r3, =FlashDictionaryEnde
  cmp r0, r3
  bhs 4f

  @ The address must be word-aligned.
  movs r2, #3
  ands r2, r0
  bne 5f

  @ Do not write to flash if the data value is 0xFFFFFFFF.
  ldr  r3, =0xFFFFFFFF
  cmp  r1, r3
  beq 2f

  @ Does the flash contain 0xFFFFFFFF?
  ldr r2, [r0]
  cmp r2, r3
  bne 6f

  @ Use real flash memory address.
  ldr r2, =0x08000000
  adds r0, r2

  @ Ready to program flash.

  @ Unlock flash
  ldr r2, =FMC_KEY
  ldr r3, =0x45670123
  str r3, [r2]
  ldr r3, =0xCDEF89AB
  str r3, [r2]

  @ Enable write
  ldr r2, =FMC_CTL
  movs r3, #1   @ Select Flash programming
  str r3, [r2]

  @ Write to Flash !
  str r1, [r0]

  @ Wait for flash BUSY flag to be cleared
  ldr r2, =FMC_STAT
  movs r0, #1
1:
  ldr r3, [r2]
  ands r3, r0
  bne 1b

  @ Lock Flash after finishing this
  ldr r2, =FMC_CTL
  movs r3, #0x80
  str r3, [r2]

2:
  bx lr
3:
  Fehler_Quit "Cannot write into the Forth core!"
4:
  Fehler_Quit "Address is outside the flash!"
5:
  Fehler_Quit "Address must be word-aligned!"
6:
  Fehler_Quit "Flash location already programmed!"

@ -----------------------------------------------------------------------------
  Wortbirne Flag_visible, "flashpageerase" @ ( Addr -- )
  @ Löscht einen 1kb großen Flashblock  Deletes one 1kb Flash page
flashpageerase:
@ -----------------------------------------------------------------------------
  push {r0, r1, r2, r3, lr}
  popda r0  @ Address of the page to erase.

  @ Make sure not to overwrite the Forth core.
  ldr r3, =Kernschutzadresse
  cmp r0, r3
  blo 2f

  @ Use real flash memory address.
  ldr r3, =0x08000000
  adds r0, r3

  @ Unlock flash
  ldr r2, =FMC_KEY
  ldr r3, =0x45670123
  str r3, [r2]
  ldr r3, =0xCDEF89AB
  str r3, [r2]

  @ Enable erase
  ldr r2, =FMC_CTL
  movs r3, #2  @ Set Erase bit
  str r3, [r2]

  @ Set page to erase
  ldr r2, =FMC_ADDR
  str r0, [r2]

  @ Start erasing
  ldr r2, =FMC_CTL
  movs r3, #0x42  @ Start + Erase
  str r3, [r2]

  @ Wait for Flash BUSY Flag to be cleared
  ldr r2, =FMC_STAT
1:
  ldr r3, [r2]
  movs r0, #1
  ands r0, r3
  bne 1b

  @ Lock Flash after finishing this
  ldr r2, =FMC_CTL
  movs r3, #0x80
  str r3, [r2]

2:
  pop {r0, r1, r2, r3, pc}

@ -----------------------------------------------------------------------------
  Wortbirne Flag_visible, "eraseflash" @ ( -- )
  @ Erase the whole flash dictionary
@ -----------------------------------------------------------------------------
  ldr r0, =FlashDictionaryAnfang
eraseflash_intern:
  cpsid i
  ldr r1, =FlashDictionaryEnde
  ldr r2, =0xFFFFFFFF
1:
  ldr r3, [r0]
  cmp r3, r2
  beq 2f
  pushda r0
  dup
  write "Erase block at  "
  bl hexdot
  writeln " from Flash"
  bl flashpageerase
2:
  adds r0, #4
  cmp r0, r1
  bne 1b
  writeln "Finished. Reset !"
  b Restart

@ -----------------------------------------------------------------------------
  Wortbirne Flag_visible, "eraseflashfrom" @ ( Addr -- )
  @ Beginnt an der angegebenen Adresse mit dem Löschen des Dictionaries.
@ -----------------------------------------------------------------------------
  popda r0
  b.n eraseflash_intern
