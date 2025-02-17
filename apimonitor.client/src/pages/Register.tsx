import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useFormik } from 'formik';
import { validationSchema } from '../validations/validationSchema';
import { authService } from '../services/authService';
import { motion } from 'framer-motion';

interface RegisterFormValues {
    email: string;
    password: string;
    confirmPassword: string;
    rememberMe: boolean;
}

const Register = () => {
    const navigate = useNavigate();
    const [errorMessage, setErrorMessage] = useState<string>('');
    const [shake, setShake] = useState(false);  // For shake animation

    const formik = useFormik<RegisterFormValues>({
        initialValues: {
            email: '',
            password: '',
            confirmPassword: '',
            rememberMe: false,
        },
        validationSchema: validationSchema,
        onSubmit: async (values) => {
            if (values.password !== values.confirmPassword) {
                setShake(true);  // Trigger the shake animation
                setTimeout(() => setShake(false), 500);  // Reset the shake after 500ms
                return;  // Do not proceed with registration
            }

            try {
                await authService.register(values.email, values.password, values.confirmPassword, values.rememberMe);
                navigate('/homepage');
            } catch (error) {
                setErrorMessage(error instanceof Error ? error.message : 'Registration failed. Please try again later.');
            }
        }
    });

    return (
        <div className="flex items-center justify-center h-screen w-screen">

            <div className="">
                <form className="">

                    <div className="flex flex-col justify-center items-center gap-x-2">
                        <label>Email</label>
                        <input />
                    </div>

                    <div className="flex flex-col justify-center items-center gap-x-2">
                        <label>Password</label>
                        <input />
                    </div>

                    <div className="flex flex-col justify-center items-center gap-x-2">
                        <label>Confirm Password</label>
                        <input />
                    </div>

                    <div className="flex flex-col justify-center items-center gap-x-2">
                        <label>Remember Me</label>
                        <input type="checkbox" />
                    </div>

                    <div className="flex flex-col justify-center items-center gap-x-2">
                        <label>Already have an account?</label>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default Register;
