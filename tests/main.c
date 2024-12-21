#include <stdio.h>

int main() {
    char str[16] = "Hello World !";
    char **p = &(&str[3]);

    return 0;
}