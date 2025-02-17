import * as yup from "yup";

export const validationSchema = yup.object({
    email: yup
        .string()
        .email('Invalid email address')
        .required('Email is required'),
    password: yup
        .string()
        .min(6, 'Password must be at least 6 characters')
        .matches(/[a-z]/, 'Password must contain at least one lowercase letter')
        .matches(/[A-Z]/, 'Password must contain at least one uppercase letter')
        .matches(/\d/, 'Password must contain at least one number')
        .required('Password is required'),
    confirmPassword: yup
        .string()
        .oneOf([yup.ref('password')], 'Passwords must match')
        .required('Confirm password is required'),
});