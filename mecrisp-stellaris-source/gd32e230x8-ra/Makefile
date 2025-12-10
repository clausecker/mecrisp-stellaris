TARGET ?= mecrisp-stellaris-gd32e230x8

ARMGNU ?= arm-none-eabi

COPS = -Wall  -Os -nostdlib -nostartfiles -ffreestanding -save-temps
AOPS = --warn --fatal-warnings

.PHONY: clean all install

all : $(TARGET).bin 

$(TARGET).o : $(TARGET).s
	$(ARMGNU)-as $(TARGET).s -o $@

$(TARGET).bin : memmap $(TARGET).o
	$(ARMGNU)-ld -o $(TARGET).elf -T memmap $(TARGET).o -Map=$(TARGET).map
	$(ARMGNU)-objdump -D $(TARGET).elf > $(TARGET).list
	$(ARMGNU)-objcopy $(TARGET).elf $@ -O binary
	$(ARMGNU)-size $(TARGET).elf

clean:
	rm -f *.bin
	rm -f *.elf
	rm -f *.list
	rm -f *.map
	rm -f *.o
	rm -f *~

# vim: set ts=4 sw=4 noexpandtab :

