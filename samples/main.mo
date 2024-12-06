define i32 @test(i32 %value)
{
    %temp0 = load i32, i32* %value
    ret i32 %temp0
}
define i32 @main()
{
    %a = alloca i32
    %temp1 = call i32 @test(i32 10)
    store i32 %temp1, i32* %a
    %temp2 = load i32, i32* %a
    ret i32 %temp2
}
