export const Badge = ({ children, color = "blue" }) => {
    const colors = {
        blue: "bg-blue-50 text-blue-700 border-blue-100",
        green: "bg-green-50 text-green-700 border-green-100",
        gray: "bg-gray-50 text-gray-700 border-gray-100",
    };
    
    return (
        <span className={`px-3 py-1 rounded-full text-xs font-semibold border ${colors[color]} transition-all hover:scale-105`}>
            {children}
        </span>
    );
};