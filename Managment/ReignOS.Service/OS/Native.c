#include <fcntl.h>

int ioctl_var_arg0(int __fd, unsigned long int __request)
{
    ioctl(__fd, __request);
}

int ioctl_var_arg1(int __fd, unsigned long int __request, int __var_arg)
{
    ioctl(__fd, __request, __var_arg);
}