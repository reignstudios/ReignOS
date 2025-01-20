#include <sys/ioctl.h>

int ioctl_var_arg0(int __fd, unsigned long int __request)
{
    ioctl(__fd, __request);
}

int ioctl_var_arg1_int(int __fd, unsigned long int __request, int __var_arg)
{
    ioctl(__fd, __request, __var_arg);
}

int ioctl_var_arg1_void(int __fd, unsigned long int __request, void* __var_arg)
{
    ioctl(__fd, __request, __var_arg);
}