/**
*  @file osdep_samD21j18a.c
*  @brief Mercury API - Sample OS-dependent functions that do nothing
*  @author Pallav Joshi
*  @date 6/14/2016
*/

#include <stdint.h>
#include "osdep.h"
#include "conf_clocks.h"

uint64_t msec=0;
static uint8_t  timerSts = 0xFF;

uint8_t start_sysTickTimer(void)
{
    // Configure SysTick to generate an interrupt every millisecond
    timerSts = SysTick_Config(system_gclk_gen_get_hz(GCLK_GENERATOR_0)/1000);
    return timerSts;
}

void 
SysTick_Handler(void)
{
	msec++;
}

uint64_t 
tmr_gettime(void)
{
  return msec;
}

uint32_t 
tmr_gettime_low()
{
	/* Fill in with code that returns the low 32 bits of a millisecond
	* counter. The API will not otherwise interpret the counter value.
	*/
	return (tmr_gettime() >>  0) & 0xffffffff;
}

uint32_t 
tmr_gettime_high()
{
	/* Fill in with code that returns the hugh 32 bits of a millisecond
	* counter. The API will not otherwise interpret the counter value.
	* Returning 0 is acceptable here if you do not have a large enough counter.
	*/
	return (tmr_gettime() >> 32) & 0xffffffff;
}

void 
tmr_sleep(uint32_t sleepms)
{
	/*
	* Fill in with code that returns after at least sleepms milliseconds
	* have elapsed.
	*/
	while (sleepms--)
	{
		SysTick_Config(SystemCoreClock/1000);
	}
}

TMR_TimeStructure
tmr_gettimestructure()
{
	TMR_TimeStructure timestructure;
	timestructure.tm_year = (uint32_t)0;
	timestructure.tm_mon = (uint32_t)0;
	timestructure.tm_mday = (uint32_t)0;
	timestructure.tm_hour = (uint32_t)0;
	timestructure.tm_min = (uint32_t)0;
	timestructure.tm_sec = (uint32_t)0;
	return timestructure;
}