\ i2c.fs I2C for the rp2040
\ currently I2C0 on pins 4 (SDA) and 5 (SCL) only

$40044000 constant I2C0_BASE
\ =========================== I2C0 =========================== \
$40044000 constant I2C0_IC_CON
$40044004 constant I2C0_IC_TAR
$40044010 constant I2C0_IC_DATA_CMD
$40044014 constant I2C0_IC_SS_SCL_HCNT \ Standard Speed I2C Clock SCL High Count Register
$40044018 constant I2C0_IC_SS_SCL_LCNT \ Standard Speed I2C Clock SCL Low Count Register
$4004401C constant I2C0_IC_FS_SCL_HCNT \ Fast Mode or Fast Mode Plus I2C Clock SCL High Count Register
$40044020 constant I2C0_IC_FS_SCL_LCNT \ Fast Mode or Fast Mode Plus I2C Clock SCL Low Count Register
$4004402C constant I2C0_IC_INTR_STAT
$40044030 constant I2C0_IC_INTR_MASK
$40044034 constant I2C0_IC_RAW_INTR_STAT
$40044038 constant I2C0_IC_RX_TL \ I2C Receive FIFO Threshold Register
$4004403C constant I2C0_IC_TX_TL \ I2C Transmit FIFO Threshold Register
$40044040 constant I2C0_IC_CLR_INTR \ Clear Combined and Individual Interrupt Register
$40044044 constant I2C0_IC_CLR_RX_UNDER \ Clear RX_UNDER Interrupt Register
$40044048 constant I2C0_IC_CLR_RX_OVER \ Clear RX_OVER Interrupt Register
$4004404C constant I2C0_IC_CLR_TX_OVER \ Clear TX_OVER Interrupt Register
$40044050 constant I2C0_IC_CLR_RD_REQ \ Clear RD_REQ Interrupt Register
$40044054 constant I2C0_IC_CLR_TX_ABRT \ Clear TX_ABRT Interrupt Register
$40044058 constant I2C0_IC_CLR_RX_DONE \ Clear RX_DONE Interrupt Register
$4004405C constant I2C0_IC_CLR_ACTIVITY \ Clear ACTIVITY Interrupt Register
$40044060 constant I2C0_IC_CLR_STOP_DET \ Clear STOP_DET Interrupt Register
$40044064 constant I2C0_IC_CLR_START_DET \ Clear START_DET Interrupt Register
$40044068 constant I2C0_IC_CLR_GEN_CALL \ Clear GEN_CALL Interrupt Register
$4004406C constant I2C0_IC_ENABLE \ I2C Enable Register
$40044070 constant I2C0_IC_STATUS
$40044074 constant I2C0_IC_TXFLR
$40044078 constant I2C0_IC_RXFLR
$4004407C constant I2C0_IC_SDA_HOLD
$40044080 constant I2C0_IC_TX_ABRT_SOURCE
$40044088 constant I2C0_IC_DMA_CR
$4004408C constant I2C0_IC_DMA_TDLR \ DMA Transmit Data Level Register
$40044090 constant I2C0_IC_DMA_RDLR \ I2C Receive Data Level Register
$40044094 constant I2C0_IC_SDA_SETUP
$4004409C constant I2C0_IC_ENABLE_STATUS
$400440A0 constant I2C0_IC_FS_SPKLEN
$400440A8 constant I2C0_IC_CLR_RESTART_DET

I2C0_BASE $0000001c + constant I2C0_IC_CLKDIV

\ =========================== RESETS =========================== \
$4000C000 constant RESETS_RESET
$4000C008 constant RESETS_RESET_DONE
$8        constant RESETS_RESET_I2C0_BITS

$40014000 constant IO_BANK0_GPIO0_STATUS \ GPIO status
$40014004 constant IO_BANK0_GPIO0_CTRL \ GPIO control including function select and overrides.
$40014008 constant IO_BANK0_GPIO1_STATUS \ GPIO status
$4001400C constant IO_BANK0_GPIO1_CTRL \ GPIO control including function select and overrides.
$40014024 constant IO_BANK0_GPIO4_CTRL \ GPIO control including function select and overrides.
$4001402C constant IO_BANK0_GPIO5_CTRL

$4001C004 constant PADS_BANK0_GPIO0 \ Pad control register
$4001C008 constant PADS_BANK0_GPIO1 \ Pad control register
$4001C014 constant PADS_BANK0_GPIO4 \ Pad control register
$4001C018 constant PADS_BANK0_GPIO5 \ Pad control register

100000 constant I2C_BAUDRATE

: i2c-reserved-addr ( addr -- flg ) $78 and dup 0= swap $78 = or ;

: disable-i2c
	%1 I2C0_IC_ENABLE bic!
	\ begin %1 I2C0_IC_ENABLE_STATUS bit@ 0= until
;

: enable-i2c
	%1 I2C0_IC_ENABLE bis!
	\ begin %1 I2C0_IC_ENABLE_STATUS bit@ until
;

: i2c-busy? ( -- flg )
	%100 I2C0_IC_ENABLE_STATUS bit@
;

: i2c-init
	\ Reset the I2C0 peripheral
	RESETS_RESET_I2C0_BITS RESETS_RESET bis!
	RESETS_RESET_I2C0_BITS RESETS_RESET bic!
	begin RESETS_RESET_I2C0_BITS RESETS_RESET_DONE bit@ until

	\ Configure GPIO pins 4 (SDA) and 5 (SCL)
    3 IO_BANK0_GPIO4_CTRL ! \ Set to I2C function
    3 IO_BANK0_GPIO5_CTRL !

    $60 PADS_BANK0_GPIO4 !  \ Enable pull-ups
    $60 PADS_BANK0_GPIO5 !

    \ Set up I2C Control register Master mode, 7-bit addressing,
    \ I2C_IC_CON_TX_EMPTY, I2C_IC_CON_IC_RESTART_EN_BITS, fast-mode
    $100 $20 or $40 or $01 or $02 1 lshift or I2C0_IC_CON !

    \ Set FIFO watermarks to 1 to make things simpler. This is encoded by a register value of 0.
	0 I2C0_IC_RX_TL !
	0 I2C0_IC_TX_TL !

	\ DMA stuff
	$02 $01 or I2C0_IC_DMA_CR !

    \ Set baud rate (Assuming 125MHz system clock)
	\ uint period = (freq_in + baudrate / 2) / baudrate;
    \ uint lcnt = period * 3 / 5; // oof this one hurts
    \ uint hcnt = period - lcnt;
    125000000 I2C_BAUDRATE 2/ + I2C_BAUDRATE / dup 	\ period
    \ dup 125000000 swap / ." baudrate = " .          \ debug
    3 * 5 / >r r@ I2C0_IC_FS_SCL_LCNT !   			\ lcnt
    r@ - I2C0_IC_FS_SCL_HCNT ! 						\ hcnt
    \ fs_spklen = lcnt < 16 ? 1 : lcnt / 16;
	r@ 16 < if 1 else r@ 16 / then I2C0_IC_FS_SPKLEN !
	rdrop

	\ sda_tx_hold_count = ((freq_in * 3) / 10000000) + 1;
	125000000 3 * 10000000 / 1+ $06 and I2C0_IC_SDA_HOLD !

    \ Enable the I2C controller
    enable-i2c
;

: i2c-set-address ( addr -- )
	disable-i2c I2C0_IC_TAR ! enable-i2c
;

\ write a buffer of data with stop at the end
: i2c-writebuf ( n buf addr -- errflg )
	2 pick 1 < if 2drop drop true exit then \ check >= 1
	i2c-set-address
	over 0 do 								\ -- n buf
		dup i + c@ 							\ -- n buf data
		2 pick 1- i = if $200 or then \ if last byte issue stop
		I2C0_IC_DATA_CMD !
		\ wait for transmit
		begin %10000 I2C0_IC_RAW_INTR_STAT bit@ until

		\ check for errors
		I2C0_IC_TX_ABRT_SOURCE @ if I2C0_IC_CLR_TX_ABRT @ drop true else false then
		if \ there was an error
			begin $200 I2C0_IC_RAW_INTR_STAT bit@ until  \ wait for stop
			I2C0_IC_CLR_STOP_DET @ drop
			2drop unloop true exit
		then
	loop
	begin $200 I2C0_IC_RAW_INTR_STAT bit@ until  \ wait for stop
	I2C0_IC_CLR_STOP_DET @ drop
	2drop
	false
;

\ write a buffer of data with no stop so transaction can continue
: i2c-writebuf-nostop ( n buf addr -- errflg )
	2 pick 1 < if 2drop drop true exit then \ check >= 1
	i2c-set-address
	swap 0 do 								\ -- buf
		dup i + c@ 							\ -- buf data
		I2C0_IC_DATA_CMD !
		\ wait for transmit
		begin %10000 I2C0_IC_RAW_INTR_STAT bit@ until

		\ check for errors
		I2C0_IC_TX_ABRT_SOURCE @ if I2C0_IC_CLR_TX_ABRT @ drop true else false then
		if \ there was an error
			begin $200 I2C0_IC_RAW_INTR_STAT bit@ until  \ wait for stop
			I2C0_IC_CLR_STOP_DET @ drop
			drop unloop true exit
		then
	loop
	drop
	false
;

: _i2c-dorcv ( buf cmd -- errflg )
		I2C0_IC_DATA_CMD !
		\ wait for recieve or error
		begin I2C0_IC_TX_ABRT_SOURCE @	I2C0_IC_RXFLR @ or until
		\ check for errors
		I2C0_IC_TX_ABRT_SOURCE @ if	I2C0_IC_CLR_TX_ABRT @ drop drop true exit then
		I2C0_IC_DATA_CMD @	\ read data byte
		swap c!		  		\ store in buffer
		false
;

false variable _i2c-restartflg

\ read a buffer of data normal start/stop
: _i2c-read ( n buf -- errflg )
	over 1- -rot						\ -- n-1 n buf
	swap 0 do  							\ -- n-1 buf
		\ not sure why but pico sdk does this - wait for space in tx fifo
		begin 16 I2C0_IC_TXFLR @ - until
		over i = if $200 else 0 then			 		\ stop flag if last byte
		i 0= if _i2c-restartflg @ if $400 or then then	\ restart flag
		$100 or  										\ read cmd
														\ -- n-1 buf cmd
		over i + swap									\ next buffer place
		_i2c-dorcv if 2drop unloop true exit then
	loop
	2drop
	false
;

: i2c-readbuf ( n buf addr -- errflg )
	2 pick 1 < if 2drop drop true exit then \ check >= 1
	i2c-set-address
	false _i2c-restartflg !
	_i2c-read
;

\ restart a read issued after writebufnostop to read a bunch of registers
\ NOTE address has already been set in writebufnostop
: i2c-readbuf-restart ( n buf -- errflg )
	over 1 < if 2drop true exit then 	\ check >= 1
	true _i2c-restartflg !
	_i2c-read
;

: i2c-deviceready? ( addr -- flg )
	i2c-set-address

	$100 			\ I2C_IC_DATA_CMD_CMD_BITS
	$200 or 		\ last byte so issue stop
	I2C0_IC_DATA_CMD !

	\ wait for recieve or error
	begin
		I2C0_IC_TX_ABRT_SOURCE @	\ error?
		I2C0_IC_RXFLR @ or			\ receive available?
	until

	\ check for errors
	I2C0_IC_TX_ABRT_SOURCE @ if
		I2C0_IC_CLR_TX_ABRT @ drop false
	else
		I2C0_IC_DATA_CMD @ drop
		true
	then
;

\ --------------------- Bus scan stuff ------------------
\ print hex value n with x bits
: N#h. ( n bits -- )
	begin
		4 -
		2dup rshift $F and .digit emit
		dup 0=
	until 2drop
;

: 2#h. ( char -- )
	8 N#h.
;

\ scan and report all I2C devices on the bus
: i2cScan. ( -- )
    i2c-init
    128 0 do
        cr i 2#h. ." :"
        16 0 do  space
          i j + i2c-deviceready? if i j + 2#h. else ." --" then
          \ i j + 2#h.
          2 ms
          key? if unloop unloop exit then
        loop
    16 +loop
    cr
;

