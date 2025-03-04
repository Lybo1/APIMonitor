import * as THREE from 'three';

declare module '@react-three/fiber' {
    namespace JSX {
        interface IntrinsicElements {
            meshStandardMaterial: React.DetailedHTMLProps<React.HTMLAttributes<THREE.MeshStandardMaterial>, THREE.MeshStandardMaterial>;
            ambientLight: React.DetailedHTMLProps<React.HTMLAttributes<THREE.AmbientLight>, THREE.AmbientLight>;
            pointLight: React.DetailedHTMLProps<React.HTMLAttributes<THREE.PointLight>, THREE.PointLight>;
            sphere: React.DetailedHTMLProps<React.HTMLAttributes<THREE.Mesh>, THREE.Mesh>;
        }
    }
}