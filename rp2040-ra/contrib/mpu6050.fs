\ mpu6050 driver

\ #require fixpt-math-lib.fs
#require i2c.fs

6 buffer: mpu6050-i2c-buf
$68 constant MPU6050_ADDR
: x-badchip ." Chip ID is incorrect" cr ;
: x-badread ." Did not read required amount of data" cr ;

: mpu6050-get-reg ( reg -- val )
	mpu6050-i2c-buf c!
	1 mpu6050-i2c-buf MPU6050_ADDR i2c-writebuf-nostop if ." getreg failed" 0 exit then
	1 mpu6050-i2c-buf i2c-readbuf-restart if ." getreg failed" 0 exit then
	mpu6050-i2c-buf c@
;

: mpu6050-set-reg ( val reg -- )
	mpu6050-i2c-buf c!
	mpu6050-i2c-buf 1+ c!
	2 mpu6050-i2c-buf MPU6050_ADDR i2c-writebuf if ." setreg failed" then
;

: mpu6050-get-regs ( n reg -- val1..valn )
	mpu6050-i2c-buf c!
	1 mpu6050-i2c-buf MPU6050_ADDR i2c-writebuf-nostop if drop ." getregs failed" $8000 exit then
	dup mpu6050-i2c-buf i2c-readbuf-restart if drop ." getregs failed" $8000 exit then
	0 do mpu6050-i2c-buf i + c@ loop
;

$75 constant MPU6050_WHO_AM_I
: mpu6050-init ( -- errflg )
    \ check chip ID
	MPU6050_WHO_AM_I mpu6050-get-reg
	1 rshift %00111111 and
	$34 <> if
		x-badchip
		true
	else
		false
	then
;

: mpu6050-setup ( -- )
	$80 $6B mpu6050-set-reg         \ reset
	begin $6B mpu6050-get-reg $80 and not until \ wait for reset
	100 ms

	0 3 lshift $1C mpu6050-set-reg 	\ ACCEL_CONFIG = 0 2g
	1 3 lshift $1B mpu6050-set-reg 	\ GYRO_CONFIG = 1  500rad/s
	100 ms

	$6B mpu6050-get-reg 			\ PWR_MGMT_1
	1 0 lshift or                   \ use gyrox as clocksel
	1 6 lshift not and				\ take out of sleep by clearing bit 6
	$6B mpu6050-set-reg 			\ PWR_MGMT_1 = $00 start chip
	100 ms
;

\ convert 16bit signed to 32bit signed
: 16>32 ( s -- s ) $8000 xor $8000 - ;

: read-acc ( -- ax ay az )
	6 $3B mpu6050-get-regs dup $8000 and if 0 0 x-badread exit then
	\ xh xl yh yl zh zl
  	swap 8 lshift or >r  \ az
  	swap 8 lshift or >r  \ ay
  	swap 8 lshift or >r  \ ax
  	r> 16>32
  	r> 16>32
  	r> 16>32
;

: read-gyro ( -- gx gy gz )
	6 $43 mpu6050-get-regs dup $8000 and if 0 0 x-badread exit then
	\ xh xl yh yl zh zl
  	swap 8 lshift or >r  \ gz
  	swap 8 lshift or >r  \ gy
  	swap 8 lshift or >r  \ gx
  	r> 16>32
  	r> 16>32
  	r> 16>32
;

: read-temp ( -- temp )
	2 $41 mpu6050-get-regs dup $8000 and if x-badread exit then
	\ h l
  	swap 8 lshift or 16>32
  	\ temperature = (rawTemp / 340.0) + 36.53;
  	s>f 340,0 f/ 36,53 d+ 0,5 d+ f>s
;

: n.6 DUP ABS 0 <# #s ROT SIGN #> dup 6 swap - spaces type ;

: test-6050
	i2c-init
	mpu6050-init if exit then
	mpu6050-setup
	cr
	begin
		read-gyro
		-rot swap
		." gx: " n.6
		." , gy: " n.6
		." , gz: " n.6
		read-acc
		-rot swap
		." , ax: " n.6
		." , ay: " n.6
		." , az: " n.6
		read-temp
		." , temp: " .
		cr
		200 ms
	key? until
;

: fr2 21474836 0 2over d0< if d- else d+ then ;
: f.r2 fr2 2 f.n ;

: angle_calc ( ax ay az -- fpangle )
	\ Acc_angle = atan2(AcY, -AcX) * 57.2958;
	drop
	s>f
	rot
	s>f dnegate
	atan2
;

: test-angle
	i2c-init
	mpu6050-init if exit then
	mpu6050-setup
	begin
		read-acc
		angle_calc
		f.r2 cr
		200 ms
	key? until
;

