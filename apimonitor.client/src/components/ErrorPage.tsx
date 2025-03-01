import React from 'react';
import { motion } from 'framer-motion';
import Lottie from 'react-lottie';
import { Howl } from 'howler';
import Particles from 'react-tsparticles';

const errorSound = new Howl({
    src: [""],
    volume: 0.5,
});

const ErrorPage: React.FC = () => {

    const handleRetryClick = () => {
        errorSound.play();
        alert("Retrying...");
    };

    return (
        <div className="relative h-screen bg-gray-800">
            <Particles
                id="tsparticles"
                options={{
                    fullScreen: {
                        enable: true,
                        zIndex: -1,
                    },
                    particles: {
                        number: {
                            value: 50,
                        },
                        size: {
                            value: 3,
                        },
                        move: {
                            enable: true,
                            speed: 2,
                        },
                        links: {
                            enable: true,
                            distance: 150,
                            color: "#ffffff",
                            opacity: 0.4,
                            width: 1,
                        },
                        interactivity: {
                            events: {
                                onhover: {
                                    enable: true,
                                    mode: "repulse",
                                },
                                onclick: {
                                    enable: true,
                                    mode: "push",
                                },
                            },
                        },
                    },
                }}
            />

            <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 text-center">
                <div className="mb-4">
                    <Lottie
                        options={{
                            loop: true,
                            autoplay: true,
                        }}
                        width={200}
                        height={200}
                    />
                </div>

                <motion.h1
                    className="text-white text-4xl md:text-5xl font-bold mb-4"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ duration: 1 }}
                >
                    Something Went Wrong!
                </motion.h1>

                <motion.p
                    className="text-lg text-gray-300 mb-6"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ duration: 1.5 }}
                >
                    We couldn't find the page you were looking for. But don't worry, we are here to help!
                </motion.p>

                <motion.button
                    onClick={handleRetryClick}
                    className="px-6 py-3 bg-blue-500 hover:bg-blue-600 text-white rounded-lg shadow-lg transform hover:scale-105 transition-all"
                    whileHover={{ scale: 1.1 }}
                >
                    Try Again
                </motion.button>
            </div>
        </div>
    );
};

export default ErrorPage;
