/*
 * Copyright 2002-2020 Intel Corporation.
 * 
 * This software is provided to you as Sample Source Code as defined in the accompanying
 * End User License Agreement for the Intel(R) Software Development Products ("Agreement")
 * section 1.L.
 * 
 * This software and the related documents are provided as is, with no express or implied
 * warranties, other than those that are expressly stated in the License.
 */

#include "asm_macros.h"

.data
zero: .float 0

.text
.global NAME(FptagInitFunc)
NAME(FptagInitFunc):
        finit
        ret

.global NAME(Fld1Func)
NAME(Fld1Func):
        fld1
        ret

.global NAME(FldzFunc)
NAME(FldzFunc):
        fldz
        ret

.global NAME(FldInfFunc)
NAME(FldInfFunc):
# We don't build Pin on 32b Mac.
# On Mac we can't use fdiv, we must use fdivl.
#ifdef TARGET_MAC
        push %rdi
        lea zero(%rip), %rdi
        fldpi
        fdivl (%rdi)
        pop %rdi
        ret
#else
        fldpi
        fdiv zero
        ret
#endif

.global NAME(DoFnstenv)
NAME(DoFnstenv):
        BEGIN_STACK_FRAME
        mov PARAM1,GAX_REG
        fnstenv (GAX_REG)
        END_STACK_FRAME
        ret

.global NAME(FstpFunc)
NAME(FstpFunc):
        fstp %st(0)
        ret
