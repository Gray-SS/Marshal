typedef struct {
} test_t;

int main() {
    test_t x;
    test_t* y = &x;

    test_t z = (test_t)y;
    return 0;
}