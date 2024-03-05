

float Sigmoid(float x)
{
    return pow(1 + exp(-x), -1);
}
float SigmoidPrime(float sigmoid_x)
{
    return sigmoid_x * (1 - sigmoid_x);
}

float Relu(float x)
{
    return max(0, x);
}
float ReluPrime(float relu_x)
{
    return relu_x > 0 ? 1 : 0;
}

float Tanh(float x)
{
    return (exp(x) - exp(-x)) / (exp(x) + exp(-x));
}
float TanhPrime(float tanh_x)
{
    return 1 - tanh_x * tanh_x;
}

float ActivationFunction(float x, int activationID)
{
    if (activationID == 0)
        return Sigmoid(x);
    else if (activationID == 1)
        return Relu(x);
    else if (activationID == 2)
        return Tanh(x);
    
    return -1;
}

float ActivationFunctionPrime(float x, int activationID)
{
    if (activationID == 0)
        return SigmoidPrime(x);
    else if (activationID == 1)
        return ReluPrime(x);
    else if (activationID == 2)
        return TanhPrime(x);
    
    return -1;
}