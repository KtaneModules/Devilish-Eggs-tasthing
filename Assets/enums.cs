public enum DEColor
{
    magenta,
    orange,
    green,
    cyan
}

public enum DECooking
{
    sunnySideUp,
    fried,
    scrambled,
    boiled
}

public enum DERotation
{
    T90CW,
    T180CW,
    T270CW,
    T360CW,
    T90CCW,
    T180CCW,
    T270CCW,
    T360CCW,
    W90CW,
    W180CW,
    W270CW,
    W360CW,
    W90CCW,
    W180CCW,
    W270CCW,
    W360CCW
}

/*
    CHECKS:
    Turn: x < 8
    Twist: x > 7
    90: x % 4 == 0
    180: x % 4 == 1
    270: x % 4 == 2
    360: x % 4 == 3
    CW:  x / 4 == 0 || x / 4 == 2
    CCW: x / 4 == 1 || x / 4 == 3
*/

public enum DEInstruction
{
    SS,
    FR,
    SC,
    BL,
    CM,
    CO,
    CG,
    CC,
    CT,
    CB,
    MA,
    MB,
    MC,
    MD,
    RE,
    IS,
    IB
}

public enum DEMovement
{
    flipVertical,
    flipHorizontal,
    flipNESW,
    flipNWSE,
    rotateCW,
    rotateCCW,
    rotate180,
    stayInPlace
}
