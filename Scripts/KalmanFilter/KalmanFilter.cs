using System;
using Godot;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<float>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<float>;

public class KalmanFilter
{
    // This is the state transition matrix that models what the world does to a subject
    private static Matrix A = Matrix.Build.DenseIdentity(3);

    // This is the matrix describing the noise of our motion model
    private static Matrix R = Matrix.Build.DenseOfDiagonalArray([Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle()]);

    // This is the matrix describing the mapping of our state to the observation
    // Identity here means that our observation always matches our state.
    private static Matrix C = Matrix.Build.DenseIdentity(3);

    // This is the matrix describing the noise of our sensor model
    private static Matrix Q = Matrix.Build.DenseOfDiagonalArray([Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle()]);

    private static Matrix I = Matrix.Build.DenseIdentity(3);

    public Vector Mu { get; private set; }
    public Matrix Sigma { get; private set; }

    public KalmanFilter(Vector mu, Matrix Sigma)
    {
        Mu = mu;
        this.Sigma = Sigma;
    }

    // u is the current velocity/angularVelocity
    // z is the current sensor model
    public void Update(Vector u, Vector? z, double dt)
    {
        // This is the state transition matrix that models what the subject does to itself through control
        Matrix B = Matrix.Build.DenseOfArray(new float[,]{
            {(float)dt * MathF.Cos(Mu[2]), 0},
            {(float)dt * MathF.Sin(Mu[2]), 0},
            {0, (float)dt}
        });

        //GD.Print(B);
        //GD.Print(u);

        //# Prediction
        Vector d_mu_t = A * Mu + B * u; // This is the change of our current state
        Matrix d_Sigma_t = A * Sigma * A.Transpose() + R; // This is the noise of the of our prediction

        if (z is null)
        {
            Mu = d_mu_t;
            Sigma = d_Sigma_t;
            return;
        }

        // Correction
        Matrix K_t = d_Sigma_t * C.Transpose() * (C * d_Sigma_t * C.Transpose() + Q).Inverse();
        Vector mu_t = d_mu_t + K_t * (z - C * d_mu_t);
        Matrix Sigma_t = (I - K_t * C) * d_Sigma_t;

        Mu = mu_t;
        Sigma = Sigma_t;
    }
}
